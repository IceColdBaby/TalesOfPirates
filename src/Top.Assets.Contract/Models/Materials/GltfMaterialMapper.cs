using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Top.Assets.Contract.Models.Extras;
using Top.Gltf;
using Top.Logging;

namespace Top.Assets.Contract.Models.Materials
{
    /// <summary>
    /// Resolves what a glTF material asks for, reading the standard fields
    /// first and letting a <see cref="MaterialExtras"/> payload refine them.
    /// A file without extras still resolves: every section is optional and an
    /// absent one means the vanilla default.
    /// </summary>
    public static class GltfMaterialMapper
    {
        public static RenderState Map(GltfMaterial material)
        {
            var name = material.Name ?? "unnamed";
            var state = new RenderState();

            if (material.DoubleSided)
            {
                state.Cull = FaceCulling.None;
            }

            switch (material.AlphaMode)
            {
                case null:
                case "OPAQUE":
                    break;
                case "MASK":
                    state.AlphaTest = true;
                    state.Cutoff = material.AlphaCutoff ?? 0.5f;
                    break;
                case "BLEND":
                    state.SrcBlend = BlendFactor.SrcAlpha;
                    state.DstBlend = BlendFactor.OneMinusSrcAlpha;
                    state.BlendEnabled = true;
                    state.ZWrite = false;
                    break;
                default:
                    Log.Warning($"material '{name}': unknown alpha mode '{material.AlphaMode}'");
                    break;
            }

            var factor = material.PbrMetallicRoughness?.BaseColorFactor;

            if (factor != null && factor.Length >= 4)
            {
                state.Opacity = factor[3];

                if (factor[0] != 1f || factor[1] != 1f || factor[2] != 1f)
                {
                    Log.Warning($"material '{name}': base color tint has no counterpart");
                }
            }

            if (MaterialExtras.TryRead(material, out var extras))
            {
                Refine(state, extras);
            }

            WarnUnmapped(material.Rest);

            if (material.PbrMetallicRoughness != null)
            {
                WarnUnmapped(material.PbrMetallicRoughness.Rest);
            }

            return state;
        }

        private static void Refine(RenderState state, MaterialExtras extras)
        {
            state.UvAnimated = extras.UvAnimation != null;
            state.OpacityAnimated = extras.OpacityAnimation != null;

            var refinement = extras.RenderState;

            if (refinement == null)
            {
                return;
            }

            state.SrcBlend = refinement.SrcBlend;
            state.DstBlend = refinement.DstBlend;
            state.BlendEnabled = refinement.BlendEnabled;
            state.ZWrite = refinement.ZWrite;
            state.Lit = refinement.Lit;
            state.Transparency = refinement.Transparency;

            if (refinement.Cull != null)
            {
                state.Cull = refinement.Cull.Value;
            }

            if (refinement.AlphaTestCutoff != null)
            {
                state.AlphaTest = true;
                state.Cutoff = refinement.AlphaTestCutoff.Value;
            }
        }

        private static void WarnUnmapped(IDictionary<string, JToken> rest)
        {
            if (rest == null)
            {
                return;
            }

            foreach (var member in rest)
            {
                if (member.Key == "extensions" && member.Value is JObject extensions)
                {
                    foreach (var extension in extensions.Properties())
                    {
                        // The target shader is unlit-capable, so the unlit extension is already honored.
                        if (extension.Name != "KHR_materials_unlit")
                        {
                            Log.Warning($"unmapped extension '{extension.Name}'");
                        }
                    }
                }
                else
                {
                    Log.Warning($"unmapped member '{member.Key}'");
                }
            }
        }
    }
}
