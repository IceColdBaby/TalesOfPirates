using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Materials
{
    /// <summary>
    /// Resolves a material's effective render state. Atoms are read first,
    /// then the transparency type supplies its blend defaults and overrides
    /// them - except for Filter, which leaves the atoms standing. The alpha
    /// ref is not read from the atoms at all: the engine replaces it on every
    /// draw, so the exported value never reaches the device.
    /// - MTLTEX_TRANSP switch, lwMtlTexAgent::BeginSet (lwResourceMgr.cpp)
    /// </summary>
    public static class RenderStateResolver
    {
        private const uint Terminator = 0xFFFFFFFF;
        private const uint AlphaFuncGreater = 5;
        private const uint DeviceAlphaRef = 129;

        /// <summary>
        /// The object owns both halves of the answer: the atoms sit on the
        /// material, the animation tracks sit beside it. Resolving from the
        /// material alone cannot see the tracks, so it cannot state the
        /// opacity rule.
        /// </summary>
        public static ResolvedRenderState Resolve(GeometryObject obj, int materialIndex)
        {
            var material = obj.Materials[materialIndex];
            var state = new ResolvedRenderState
            {
                UvAnimated = HasUvAnimation(obj, materialIndex),
                OpacityAnimated = HasOpacityAnimation(obj, materialIndex),
            };

            uint alphaFunc = AlphaFuncGreater;

            if (material.RenderStates != null)
            {
                foreach (var atom in material.RenderStates)
                {
                    switch (atom.State)
                    {
                        case Terminator:
                            break;
                        case D3DRenderState.ZWriteEnable:
                            state.ZWrite = atom.Value0 != 0;
                            break;
                        case D3DRenderState.AlphaTestEnable:
                            state.AlphaTest = atom.Value0 != 0;
                            break;
                        case D3DRenderState.SourceBlend:
                            state.SrcBlend = ToBlend(atom.Value0, state);
                            break;
                        case D3DRenderState.DestinationBlend:
                            state.DstBlend = ToBlend(atom.Value0, state);
                            break;
                        case D3DRenderState.CullMode:
                            state.Cull = ToCull(atom.Value0, state);
                            break;
                        // Read and discarded: see EffectiveAlphaRef.
                        case D3DRenderState.AlphaRef:
                            break;
                        case D3DRenderState.AlphaFunc:
                            alphaFunc = atom.Value0;
                            break;
                        case D3DRenderState.AlphaBlendEnable:
                            state.BlendEnabled = atom.Value0 != 0;
                            break;
                        case D3DRenderState.Lighting:
                            state.Lit = atom.Value0 != 0;
                            break;
                        case 23:
                        case 26:
                        case 28:
                            break;
                        default:
                            state.Warnings.Add($"unmapped render state {atom.State}={atom.Value0}");
                            break;
                    }
                }
            }

            state.Opacity = material.Opacity;
            state.Transparency = material.Transparency;
            ApplyTransparencyDefaults(state, material.Transparency);

            if (state.AlphaTest)
            {
                // The older material layouts have their func replaced with
                // GREATER as they are read, and no later layout stores
                // anything else, so nothing else is expected here.
                // - lwMtlTexInfo_Load (lwExpObj.cpp)
                if (alphaFunc != AlphaFuncGreater)
                {
                    state.Warnings.Add($"unsupported alpha func {alphaFunc}");
                }

                // Testing for greater-than means the lowest passing alpha is
                // one step above the ref, which is where a cutoff sits.
                state.Cutoff = (EffectiveAlphaRef(state.Opacity) + 1) / 255f;
            }

            var stage0 = material.Stages != null && material.Stages.Length > 0
                ? material.Stages[0]
                : null;
            state.TextureFile = stage0 != null && !string.IsNullOrEmpty(stage0.FileName)
                ? stage0.FileName
                : null;

            return state;
        }

        /// <summary>
        /// Only stage 0 is considered, matching the stage the resolved
        /// texture comes from.
        /// </summary>
        private static bool HasUvAnimation(GeometryObject obj, int materialIndex)
        {
            var tracks = obj.Animation?.TextureUv;

            if (tracks == null || materialIndex >= tracks.GetLength(0))
            {
                return false;
            }

            var animation = tracks[materialIndex, 0];

            return animation?.Frames != null && animation.Frames.Length > 0;
        }

        private static bool HasOpacityAnimation(GeometryObject obj, int materialIndex)
        {
            var tracks = obj.Animation?.MaterialOpacity;

            if (tracks == null || materialIndex >= tracks.Length)
            {
                return false;
            }

            var animation = tracks[materialIndex];

            return animation?.Keys != null && animation.Keys.Length > 0;
        }

        /// <summary>
        /// The alpha ref the device receives. The engine rewrites every
        /// exported ref before applying the state: a fixed value, or one
        /// derived from opacity when opacity is below one.
        /// - lwMtlTexAgent::BeginSet (lwResourceMgr.cpp)
        /// </summary>
        private static uint EffectiveAlphaRef(float opacity)
        {
            if (opacity == 1f)
            {
                return DeviceAlphaRef;
            }

            // The engine's arithmetic is unsigned, so an opacity below one
            // step underflows and clamps back to the fixed value rather than
            // reaching zero. Kept as it behaves, not as it reads.
            var value = (uint)(opacity * 255) - 1;

            return value > DeviceAlphaRef ? DeviceAlphaRef : value;
        }

        private static void ApplyTransparencyDefaults(
            ResolvedRenderState state, TransparencyType transparency)
        {
            switch (transparency)
            {
                case TransparencyType.Filter:
                    break;
                case TransparencyType.Additive:
                    SetBlend(state, D3DBlend.One, D3DBlend.One);
                    break;
                case TransparencyType.Additive1:
                    SetBlend(state, D3DBlend.SrcColor, D3DBlend.One);
                    break;
                case TransparencyType.Additive2:
                    SetBlend(state, D3DBlend.SrcColor, D3DBlend.InvSrcColor);
                    break;
                case TransparencyType.Additive3:
                    SetBlend(state, D3DBlend.SrcAlpha, D3DBlend.DestAlpha);
                    break;
                case TransparencyType.Subtractive:
                    SetBlend(state, D3DBlend.Zero, D3DBlend.InvSrcColor);
                    break;
                default:
                    SetBlend(state, D3DBlend.One, D3DBlend.One);
                    state.Warnings.Add($"transparency {transparency} has no reference mapping");
                    break;
            }
        }

        private static void SetBlend(ResolvedRenderState state, D3DBlend src, D3DBlend dst)
        {
            state.SrcBlend = src;
            state.DstBlend = dst;
            state.BlendEnabled = true;
        }

        private static D3DBlend ToBlend(uint value, ResolvedRenderState state)
        {
            // D3DBlend runs contiguously from Zero to InvDestColor.
            if (value >= (uint)D3DBlend.Zero && value <= (uint)D3DBlend.InvDestColor)
            {
                return (D3DBlend)value;
            }

            state.Warnings.Add($"unmapped blend factor {value}");

            return D3DBlend.One;
        }

        private static D3DCull ToCull(uint value, ResolvedRenderState state)
        {
            if (value >= (uint)D3DCull.None && value <= (uint)D3DCull.Ccw)
            {
                return (D3DCull)value;
            }

            state.Warnings.Add($"unmapped cull mode {value}");

            return D3DCull.Ccw;
        }
    }
}
