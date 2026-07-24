using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Assets.Conversion.Materials;
using Top.Gltf;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
{
    public class GltfMaterialMapperTests
    {
        [Test]
        public void Base_color_factor_alpha_becomes_opacity()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial
            {
                PbrMetallicRoughness = new GltfPbrMetallicRoughness
                {
                    BaseColorFactor = new[] { 1f, 1f, 1f, 0.3f },
                },
            });

            Assert.That(state.Opacity, Is.EqualTo(0.3f).Within(1e-6f));
            Assert.That(state.OpacityDriven, Is.True);
            Assert.That(state.Warnings, Is.Empty);
        }

        [Test]
        public void Base_color_tint_has_no_counterpart_and_warns()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial
            {
                PbrMetallicRoughness = new GltfPbrMetallicRoughness
                {
                    BaseColorFactor = new[] { 1f, 0f, 0f, 1f },
                },
            });

            Assert.That(state.Warnings, Has.Some.Contains("tint"));
        }

        [Test]
        public void Opaque_material_keeps_defaults()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial());

            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.AlphaTest, Is.False);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Cull, Is.EqualTo(D3DCull.Ccw));
            Assert.That(state.OpacityDriven, Is.False);
            Assert.That(state.Warnings, Is.Empty);
        }

        [Test]
        public void Mask_maps_to_alpha_test()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial
            {
                AlphaMode = "MASK",
                AlphaCutoff = 0.4f,
            });

            Assert.That(state.AlphaTest, Is.True);
            Assert.That(state.Cutoff, Is.EqualTo(0.4f));
        }

        [Test]
        public void Mask_without_cutoff_uses_gltf_default()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial { AlphaMode = "MASK" });

            Assert.That(state.Cutoff, Is.EqualTo(0.5f));
        }

        [Test]
        public void Blend_maps_to_alpha_blend()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial { AlphaMode = "BLEND" });

            Assert.That(state.BlendEnabled, Is.True);
            Assert.That(state.SrcBlend, Is.EqualTo(D3DBlend.SrcAlpha));
            Assert.That(state.DstBlend, Is.EqualTo(D3DBlend.InvSrcAlpha));
            Assert.That(state.ZWrite, Is.False);
        }

        [Test]
        public void Double_sided_disables_culling()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial { DoubleSided = true });

            Assert.That(state.Cull, Is.EqualTo(D3DCull.None));
        }

        [Test]
        public void Unmapped_members_warn()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial
            {
                Rest = new Dictionary<string, JToken>
                {
                    ["normalTexture"] = new JObject(),
                    ["extensions"] = new JObject(
                        new JProperty("KHR_materials_unlit", new JObject()),
                        new JProperty("KHR_materials_emissive_strength", new JObject())),
                },
                PbrMetallicRoughness = new GltfPbrMetallicRoughness
                {
                    Rest = new Dictionary<string, JToken>
                    {
                        ["baseColorFactor"] = new JArray(1f, 0f, 0f, 1f),
                    },
                },
            });

            Assert.That(state.Warnings, Is.EquivalentTo(new[]
            {
                "unmapped member 'normalTexture'",
                "unmapped extension 'KHR_materials_emissive_strength'",
                "unmapped member 'baseColorFactor'",
            }));
        }
    }
}
