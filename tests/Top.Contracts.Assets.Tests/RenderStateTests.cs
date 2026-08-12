using NUnit.Framework;
using Top.Contracts.Assets.Models.Materials;

namespace Top.Contracts.Assets.Tests
{
    public class RenderStateTests
    {
        [Test]
        public void An_opaque_surface_writes_the_frame_whole()
        {
            var state = new RenderState();

            Assert.That(state.EffectiveSrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.EffectiveDstBlend, Is.EqualTo(BlendFactor.Zero));
            Assert.That(state.EffectiveZWrite, Is.True);
        }

        [Test]
        public void Static_opacity_turns_an_opaque_surface_into_an_alpha_blend()
        {
            var state = new RenderState { Opacity = 0.99f };

            Assert.That(state.EffectiveSrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(state.EffectiveDstBlend, Is.EqualTo(BlendFactor.OneMinusSrcAlpha));
            Assert.That(state.EffectiveZWrite, Is.False);
        }

        [Test]
        public void An_opacity_track_turns_an_opaque_surface_into_an_alpha_blend()
        {
            var state = new RenderState { OpacityAnimated = true };

            Assert.That(state.EffectiveSrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(state.EffectiveDstBlend, Is.EqualTo(BlendFactor.OneMinusSrcAlpha));
            Assert.That(state.EffectiveZWrite, Is.False);
        }

        [Test]
        public void A_blending_surface_keeps_the_factors_it_asked_for()
        {
            var state = new RenderState
            {
                SrcBlend = BlendFactor.SrcColor,
                DstBlend = BlendFactor.One,
                BlendEnabled = true,
            };

            Assert.That(state.EffectiveSrcBlend, Is.EqualTo(BlendFactor.SrcColor));
            Assert.That(state.EffectiveDstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.EffectiveZWrite, Is.True, "a blend alone does not cost the depth write");
        }

        [Test]
        public void Opacity_takes_over_the_source_factor_of_an_additive_surface()
        {
            var state = new RenderState
            {
                Transparency = TransparencyMode.Additive,
                SrcBlend = BlendFactor.One,
                DstBlend = BlendFactor.One,
                BlendEnabled = true,
                OpacityAnimated = true,
            };

            Assert.That(state.EffectiveSrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(state.EffectiveDstBlend, Is.EqualTo(BlendFactor.One), "the takeover stops at the source");
            Assert.That(state.EffectiveZWrite, Is.False);
        }

        [Test]
        public void The_reserved_additive_modes_keep_their_source_factor()
        {
            var state = new RenderState
            {
                Transparency = TransparencyMode.Additive1,
                SrcBlend = BlendFactor.SrcColor,
                DstBlend = BlendFactor.One,
                BlendEnabled = true,
                OpacityAnimated = true,
            };

            Assert.That(state.EffectiveSrcBlend, Is.EqualTo(BlendFactor.SrcColor));
        }

        [Test]
        public void A_surface_that_asks_for_no_depth_write_does_not_get_one_back()
        {
            var state = new RenderState { ZWrite = false };

            Assert.That(state.EffectiveZWrite, Is.False);
        }
    }
}
