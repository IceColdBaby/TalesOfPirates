using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Assets.Contract.Models.Extras;
using Top.Assets.Contract.Models.Materials;
using Top.Gltf;

namespace Top.Assets.Contract.Tests
{
    public class GltfMaterialMapperTests
    {
        private static GltfMaterial Carrying(MaterialExtras extras)
        {
            var builder = new GltfBuilder("scene");

            extras.Write(builder.AddMaterial("mat"));

            return builder.Document.Materials[0];
        }

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
        }

        [Test]
        public void Opaque_material_keeps_defaults()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial());

            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.AlphaTest, Is.False);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Cull, Is.EqualTo(FaceCulling.Back));
            Assert.That(state.OpacityDriven, Is.False);
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
            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.OneMinusSrcAlpha));
            Assert.That(state.ZWrite, Is.False);
        }

        [Test]
        public void Double_sided_disables_culling()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial { DoubleSided = true });

            Assert.That(state.Cull, Is.EqualTo(FaceCulling.None));
        }

        [Test]
        public void Extras_refine_the_blend_the_standard_fields_could_only_approximate()
        {
            var material = Carrying(new MaterialExtras
            {
                RenderState = new RenderStateExtras
                {
                    SrcBlend = BlendFactor.One,
                    DstBlend = BlendFactor.One,
                    BlendEnabled = true,
                    Transparency = TransparencyMode.Additive,
                },
            });

            material.AlphaMode = "BLEND";

            var state = GltfMaterialMapper.Map(material);

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.BlendEnabled, Is.True);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Transparency, Is.EqualTo(TransparencyMode.Additive));
        }

        [Test]
        public void An_empty_render_state_section_takes_the_material_back_to_the_vanilla_defaults()
        {
            var material = Carrying(new MaterialExtras { RenderState = new RenderStateExtras() });

            material.AlphaMode = "BLEND";
            material.PbrMetallicRoughness.BaseColorFactor = new[] { 1f, 1f, 1f, 0.4f };

            var state = GltfMaterialMapper.Map(material);

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.Zero));
            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Opacity, Is.EqualTo(0.4f).Within(1e-6f));
            Assert.That(state.OpacityDriven, Is.True, "opacity alone still drives the blend");
        }

        [Test]
        public void Front_face_culling_only_the_extras_can_say_is_honored()
        {
            var state = GltfMaterialMapper.Map(Carrying(new MaterialExtras
            {
                RenderState = new RenderStateExtras { Cull = FaceCulling.Front },
            }));

            Assert.That(state.Cull, Is.EqualTo(FaceCulling.Front));
        }

        [Test]
        public void An_alpha_test_cutoff_survives_alongside_a_blend()
        {
            var material = Carrying(new MaterialExtras
            {
                RenderState = new RenderStateExtras
                {
                    BlendEnabled = true,
                    AlphaTestCutoff = 0.32f,
                },
            });

            material.AlphaMode = "BLEND";

            var state = GltfMaterialMapper.Map(material);

            Assert.That(state.AlphaTest, Is.True);
            Assert.That(state.Cutoff, Is.EqualTo(0.32f).Within(1e-6f));
            Assert.That(state.BlendEnabled, Is.True);
        }

        [Test]
        public void An_unlit_material_is_extras_only_knowledge()
        {
            Assert.That(GltfMaterialMapper.Map(new GltfMaterial()).Lit, Is.True);

            Assert.That(GltfMaterialMapper.Map(Carrying(new MaterialExtras
            {
                RenderState = new RenderStateExtras { Lit = false },
            })).Lit, Is.False);
        }

        [Test]
        public void Animation_sections_mark_the_state_as_driven()
        {
            var state = GltfMaterialMapper.Map(Carrying(new MaterialExtras
            {
                UvAnimation = new UvAnimationExtras { Frames = [[1f, 0f, 0f, 1f, 0f, 0f]] },
                OpacityAnimation = new OpacityAnimationExtras { KeyFrames = [0], Values = [1f] },
            }));

            Assert.That(state.UvAnimated, Is.True);
            Assert.That(state.OpacityAnimated, Is.True);
            Assert.That(state.OpacityDriven, Is.True);
        }

        [Test]
        public void A_material_without_extras_reads_as_unanimated()
        {
            var state = GltfMaterialMapper.Map(new GltfMaterial());

            Assert.That(state.UvAnimated, Is.False);
            Assert.That(state.OpacityAnimated, Is.False);
        }
    }
}
