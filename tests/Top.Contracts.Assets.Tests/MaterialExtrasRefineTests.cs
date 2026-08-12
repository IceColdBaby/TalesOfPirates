using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Contracts.Assets.Models.Extras;
using Top.Contracts.Assets.Models.Materials;

namespace Top.Contracts.Assets.Tests
{
    /// <summary>
    /// What a payload does to a render state the standard glTF fields already
    /// resolved. Every case goes through a written and reread payload, so an
    /// absent field refines the way it would coming off a file.
    /// </summary>
    public class MaterialExtrasRefineTests
    {
        private static RenderState Refined(RenderState state, MaterialExtras extras)
        {
            var container = new JObject { [MaterialExtras.Key] = extras.ToJson() };

            Assert.That(MaterialExtras.TryRead(container, out var read), Is.True);

            read.Refine(state);

            return state;
        }

        private static RenderState FromAlphaBlend()
        {
            return new RenderState
            {
                SrcBlend = BlendFactor.SrcAlpha,
                DstBlend = BlendFactor.OneMinusSrcAlpha,
                BlendEnabled = true,
                ZWrite = false,
            };
        }

        [Test]
        public void Extras_refine_the_blend_the_standard_fields_could_only_approximate()
        {
            var state = Refined(FromAlphaBlend(), new MaterialExtras
            {
                RenderState = new RenderStateExtras
                {
                    SrcBlend = BlendFactor.One,
                    DstBlend = BlendFactor.One,
                    BlendEnabled = true,
                    Transparency = TransparencyMode.Additive,
                },
            });

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.BlendEnabled, Is.True);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Transparency, Is.EqualTo(TransparencyMode.Additive));
        }

        [Test]
        public void An_empty_render_state_section_takes_the_material_back_to_the_vanilla_defaults()
        {
            var blended = FromAlphaBlend();

            blended.Opacity = 0.4f;

            var state = Refined(blended, new MaterialExtras { RenderState = new RenderStateExtras() });

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
            var state = Refined(new RenderState(), new MaterialExtras
            {
                RenderState = new RenderStateExtras { Cull = FaceCulling.Front },
            });

            Assert.That(state.Cull, Is.EqualTo(FaceCulling.Front));
        }

        [Test]
        public void An_alpha_test_cutoff_survives_alongside_a_blend()
        {
            var state = Refined(FromAlphaBlend(), new MaterialExtras
            {
                RenderState = new RenderStateExtras
                {
                    BlendEnabled = true,
                    AlphaTestCutoff = 0.32f,
                },
            });

            Assert.That(state.AlphaTest, Is.True);
            Assert.That(state.Cutoff, Is.EqualTo(0.32f).Within(1e-6f));
            Assert.That(state.BlendEnabled, Is.True);
        }

        [Test]
        public void An_unlit_material_is_extras_only_knowledge()
        {
            Assert.That(new RenderState().Lit, Is.True);

            Assert.That(Refined(new RenderState(), new MaterialExtras
            {
                RenderState = new RenderStateExtras { Lit = false },
            }).Lit, Is.False);
        }

        [Test]
        public void Animation_sections_mark_the_state_as_driven()
        {
            var state = Refined(new RenderState(), new MaterialExtras
            {
                UvAnimation = new UvAnimationExtras { Frames = [[1f, 0f, 0f, 1f, 0f, 0f]] },
                OpacityAnimation = new OpacityAnimationExtras { KeyFrames = [0], Values = [1f] },
            });

            Assert.That(state.UvAnimated, Is.True);
            Assert.That(state.OpacityAnimated, Is.True);
            Assert.That(state.OpacityDriven, Is.True);
        }

        [Test]
        public void A_payload_without_animation_sections_reads_as_unanimated()
        {
            var state = Refined(new RenderState(), new MaterialExtras());

            Assert.That(state.UvAnimated, Is.False);
            Assert.That(state.OpacityAnimated, Is.False);
        }
    }
}
