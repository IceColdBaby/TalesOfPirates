using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Top.Gltf;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Materials
{
    /// <summary>
    /// Resolves render state from the subset of it a glTF material can
    /// express: alpha mode, cutoff, the double-sided flag, and the base color
    /// factor's alpha. glTF members with no counterpart are reported as
    /// warnings, never applied silently.
    /// This reads foreign documents - a Blender export, a modder's asset -
    /// where the material section is the only truth available. Documents this
    /// project emits carry their render state elsewhere, and their materials
    /// are matched by name instead.
    /// </summary>
    public static class GltfMaterialMapper
    {
        public static ResolvedRenderState Map(GltfMaterial material)
        {
            var state = new ResolvedRenderState();

            if (material.DoubleSided)
            {
                state.Cull = D3DCull.None;
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
                    // The same two factors the Unity enum calls SrcAlpha and
                    // OneMinusSrcAlpha.
                    state.SrcBlend = D3DBlend.SrcAlpha;
                    state.DstBlend = D3DBlend.InvSrcAlpha;
                    state.BlendEnabled = true;
                    state.ZWrite = false;
                    break;
                default:
                    state.Warnings.Add($"unknown alpha mode '{material.AlphaMode}'");
                    break;
            }

            var factor = material.PbrMetallicRoughness?.BaseColorFactor;

            if (factor != null && factor.Length >= 4)
            {
                state.Opacity = factor[3];

                if (factor[0] != 1f || factor[1] != 1f || factor[2] != 1f)
                {
                    state.Warnings.Add("base color tint has no counterpart");
                }
            }

            WarnUnmapped(material.Rest, state);

            if (material.PbrMetallicRoughness != null)
            {
                WarnUnmapped(material.PbrMetallicRoughness.Rest, state);
            }

            return state;
        }

        private static void WarnUnmapped(IDictionary<string, JToken> rest, ResolvedRenderState state)
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
                        // The target shader is unlit-capable, so the unlit
                        // extension is already honored.
                        if (extension.Name != "KHR_materials_unlit")
                        {
                            state.Warnings.Add($"unmapped extension '{extension.Name}'");
                        }
                    }
                }
                else
                {
                    state.Warnings.Add($"unmapped member '{member.Key}'");
                }
            }
        }
    }
}
