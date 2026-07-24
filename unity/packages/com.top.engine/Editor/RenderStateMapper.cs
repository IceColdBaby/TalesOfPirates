using System.Collections.Generic;
using Top.Assets.Conversion.Materials;
using Top.MindPower.Geometry;
using UnityEngine;
using UnityEngine.Rendering;

namespace Top.Engine.Editor
{
    public sealed class MaterialSpec
    {
        public string TextureFile;
        public BlendMode SrcBlend = BlendMode.One;
        public BlendMode DstBlend = BlendMode.Zero;
        public bool BlendEnabled;
        public bool ZWrite = true;
        public CullMode Cull = CullMode.Back;
        public bool AlphaTest;
        public float Cutoff = 0.5f;
        public bool UvAnimated;
        public bool Lit = true;
        public float Opacity = 1f;

        /// <summary>
        /// Copied from the resolved state, which owns the rule.
        /// </summary>
        public bool OpacityDriven;
        public TransparencyType Transparency;
        public readonly List<string> Warnings = new List<string>();

        public int RenderQueue => BlendEnabled || OpacityDriven
            ? 3000
            : AlphaTest ? 2450 : 2000;
    }

    /// <summary>
    /// Projects a resolved render state onto a Top/Legacy configuration.
    /// Reading the render-state atoms is RenderStateResolver's job; what
    /// remains here is the step that needs the engine: D3D blend and cull
    /// factors become their Unity counterparts, which carry different
    /// numeric values and are what the shader reads.
    /// </summary>
    public static class RenderStateMapper
    {
        public static MaterialSpec ToSpec(ResolvedRenderState state)
        {
            var spec = new MaterialSpec
            {
                TextureFile = state.TextureFile,
                SrcBlend = ToUnity(state.SrcBlend),
                DstBlend = ToUnity(state.DstBlend),
                BlendEnabled = state.BlendEnabled,
                ZWrite = state.ZWrite,
                Cull = ToUnity(state.Cull),
                AlphaTest = state.AlphaTest,
                Cutoff = state.Cutoff,
                Lit = state.Lit,
                Opacity = state.Opacity,
                UvAnimated = state.UvAnimated,
                OpacityDriven = state.OpacityDriven,
                Transparency = state.Transparency,
            };

            spec.Warnings.AddRange(state.Warnings);

            return spec;
        }

        public static Material CreateMaterial(MaterialSpec spec, Texture2D texture)
        {
            var material = new Material(Shader.Find("Top/Legacy"));
            material.SetFloat("_SrcBlend",
                (float)(spec.BlendEnabled ? spec.SrcBlend : BlendMode.One));
            material.SetFloat("_DstBlend",
                (float)(spec.BlendEnabled ? spec.DstBlend : BlendMode.Zero));
            material.SetFloat("_ZWrite", spec.ZWrite ? 1f : 0f);
            material.SetFloat("_Cull", (float)spec.Cull);
            if (spec.AlphaTest)
            {
                material.SetFloat("_AlphaTest", 1f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_Cutoff", spec.Cutoff);
            }

            if (spec.UvAnimated)
            {
                material.SetFloat("_UvAnimated", 1f);
            }

            if (!spec.Lit)
            {
                material.SetFloat("_Unlit", 1f);
                material.EnableKeyword("_UNLIT_ON");
            }

            if (spec.OpacityDriven)
            {
                // The engine stops writing depth and modulates alpha while
                // opacity differs from one, animated or not (lwMtlTexAgent);
                // a state-opaque material needs the filter blend for the
                // modulation to show, and an additive one needs its source
                // blend rewritten to SrcAlpha, as the engine does. Other
                // additive variants stay untouched, matching the engine's
                // reserved branch.
                if (!spec.BlendEnabled)
                {
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                }
                else if (spec.Transparency == TransparencyType.Additive)
                {
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                }

                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_Opacity", spec.Opacity);
            }

            if (spec.BlendEnabled || spec.OpacityDriven)
            {
                // Blended surfaces are emissive-style content: attenuating them
                // in shadow or projecting their quads onto receivers reads wrong.
                material.SetFloat("_ReceiveShadows", 0f);
                material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                material.SetFloat("_CastShadows", 0f);
                material.EnableKeyword("_CAST_SHADOWS_OFF");
            }

            material.mainTexture = texture;
            material.renderQueue = spec.RenderQueue;
            return material;
        }

        public static BlendMode ToUnity(D3DBlend blend)
        {
            switch (blend)
            {
                case D3DBlend.Zero: return BlendMode.Zero;
                case D3DBlend.SrcColor: return BlendMode.SrcColor;
                case D3DBlend.InvSrcColor: return BlendMode.OneMinusSrcColor;
                case D3DBlend.SrcAlpha: return BlendMode.SrcAlpha;
                case D3DBlend.InvSrcAlpha: return BlendMode.OneMinusSrcAlpha;
                case D3DBlend.DestAlpha: return BlendMode.DstAlpha;
                case D3DBlend.InvDestAlpha: return BlendMode.OneMinusDstAlpha;
                case D3DBlend.DestColor: return BlendMode.DstColor;
                case D3DBlend.InvDestColor: return BlendMode.OneMinusDstColor;
                default: return BlendMode.One;
            }
        }

        public static CullMode ToUnity(D3DCull cull)
        {
            switch (cull)
            {
                case D3DCull.None: return CullMode.Off;
                case D3DCull.Cw: return CullMode.Front;
                default: return CullMode.Back;
            }
        }
    }
}
