using System.Numerics;
using NUnit.Framework;
using Top.Assets.Contract.Models.Materials;
using Top.Assets.Conversion.Models.Materials;
using Top.MindPower;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
{
    public class RenderStateResolverTests
    {
        [Test]
        public void Additive_maps_to_one_one_blend()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive,
            }), 0);

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.BlendEnabled, Is.True);
        }

        [Test]
        public void Alpha_test_ignores_the_exported_ref()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates =
                [
                    new RenderStateAtom { State = 15, Value0 = 1 },
                    new RenderStateAtom { State = 25, Value0 = 5 },
                    new RenderStateAtom { State = 24, Value0 = 0 }
                ],
            }), 0);

            Assert.That(state.AlphaTest, Is.True);
            Assert.That(state.Cutoff, Is.EqualTo(130f / 255f).Within(1e-5f),
                "the device ref is fixed; the exported 0 never reaches it");
            Assert.That(state.BlendEnabled, Is.False);
        }

        [Test]
        public void Zwrite_and_cull_atoms_override_defaults()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive,
                RenderStates =
                [
                    new RenderStateAtom { State = 14, Value0 = 0 },
                    new RenderStateAtom { State = 22, Value0 = 1 }
                ],
            }), 0);

            Assert.That(state.ZWrite, Is.False);
            Assert.That(state.Cull, Is.EqualTo(FaceCulling.None));
        }

        [Test]
        public void Transparency_type_overrides_blend_factor_atoms()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive,
                RenderStates =
                [
                    new RenderStateAtom { State = 19, Value0 = 5 },
                    new RenderStateAtom { State = 20, Value0 = 6 }
                ],
            }), 0);

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.BlendEnabled, Is.True);
        }

        [Test]
        public void Filter_keeps_blend_factor_atoms()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates =
                [
                    new RenderStateAtom { State = 19, Value0 = 5 },
                    new RenderStateAtom { State = 20, Value0 = 6 },
                    new RenderStateAtom { State = 27, Value0 = 1 }
                ],
            }), 0);

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.OneMinusSrcAlpha));
            Assert.That(state.BlendEnabled, Is.True);
        }

        [Test]
        public void Transparency_defaults_match_reference_engine_table()
        {
            Assert.That(RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
            }), 0).BlendEnabled, Is.False);

            var additive1 = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive1,
            }), 0);
            Assert.That(additive1.SrcBlend, Is.EqualTo(BlendFactor.SrcColor));
            Assert.That(additive1.DstBlend, Is.EqualTo(BlendFactor.One));

            var additive2 = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive2,
            }), 0);
            Assert.That(additive2.SrcBlend, Is.EqualTo(BlendFactor.SrcColor));
            Assert.That(additive2.DstBlend, Is.EqualTo(BlendFactor.OneMinusSrcColor));

            var additive3 = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive3,
            }), 0);
            Assert.That(additive3.SrcBlend, Is.EqualTo(BlendFactor.SrcAlpha));
            Assert.That(additive3.DstBlend, Is.EqualTo(BlendFactor.DstAlpha));

            var subtractive = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Subtractive,
            }), 0);
            Assert.That(subtractive.SrcBlend, Is.EqualTo(BlendFactor.Zero));
            Assert.That(subtractive.DstBlend, Is.EqualTo(BlendFactor.OneMinusSrcColor));

            var subtractive1 = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Subtractive1,
            }), 0);
            Assert.That(subtractive1.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(subtractive1.DstBlend, Is.EqualTo(BlendFactor.One));
        }

        [Test]
        public void Cull_modes_resolve_cw_and_ccw()
        {
            Assert.That(RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = [new RenderStateAtom { State = 22, Value0 = 2 }],
            }), 0).Cull, Is.EqualTo(FaceCulling.Front));

            Assert.That(RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = [new RenderStateAtom { State = 22, Value0 = 3 }],
            }), 0).Cull, Is.EqualTo(FaceCulling.Back));
        }

        [Test]
        public void Opacity_below_the_clamp_lowers_the_cutoff()
        {
            var state = RenderStateResolver.Resolve(AlphaTestedObject(opacity: 0.3f), 0);

            // (uint)(0.3 * 255) - 1 = 75, one step below the cutoff.
            Assert.That(state.Cutoff, Is.EqualTo(76f / 255f).Within(1e-5f));
        }

        [Test]
        public void Opacity_above_the_clamp_keeps_the_device_alpha_ref()
        {
            Assert.That(RenderStateResolver.Resolve(AlphaTestedObject(opacity: 0.99f), 0).Cutoff,
                Is.EqualTo(130f / 255f).Within(1e-5f));

            // The engine's unsigned arithmetic underflows here rather than
            // reaching zero, so the smallest opacities clamp back up.
            Assert.That(RenderStateResolver.Resolve(AlphaTestedObject(opacity: 0.001f), 0).Cutoff,
                Is.EqualTo(130f / 255f).Within(1e-5f));
        }

        [Test]
        public void Alpha_func_is_ignored_without_alpha_test()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = [new RenderStateAtom { State = 25, Value0 = 7 }],
            }), 0);

            Assert.That(state.AlphaTest, Is.False);
        }

        [Test]
        public void Uv_animation_is_read_from_stage_zero()
        {
            var obj = Object(new MaterialTexture { Transparency = TransparencyType.Filter });

            obj.Animation = new AnimationData { TextureUv = new TextureUvAnimation[1, 4] };
            obj.Animation.TextureUv[0, 1] = new TextureUvAnimation { Frames = new Matrix4x4[4] };

            Assert.That(RenderStateResolver.Resolve(obj, 0).UvAnimated, Is.False,
                "the resolved texture comes from stage 0, so only stage 0 counts");

            obj.Animation.TextureUv[0, 0] = new TextureUvAnimation { Frames = new Matrix4x4[4] };

            Assert.That(RenderStateResolver.Resolve(obj, 0).UvAnimated, Is.True);
        }

        [Test]
        public void An_opacity_track_drives_rendering_at_full_opacity()
        {
            var obj = Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                Opacity = 1f,
            });

            Assert.That(RenderStateResolver.Resolve(obj, 0).OpacityDriven, Is.False);

            obj.Animation = new AnimationData
            {
                MaterialOpacity =
                [
                    new MaterialOpacityAnimation { Keys = new FloatKeyframe[2] }
                ],
            };

            var state = RenderStateResolver.Resolve(obj, 0);

            Assert.That(state.OpacityAnimated, Is.True);
            Assert.That(state.OpacityDriven, Is.True,
                "the track moves opacity off one even though the stored value is one");
        }

        [Test]
        public void Static_opacity_below_one_drives_rendering_without_a_track()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                Opacity = 0.5f,
            }), 0);

            Assert.That(state.OpacityAnimated, Is.False);
            Assert.That(state.OpacityDriven, Is.True);
        }

        [Test]
        public void Missing_animation_data_reports_no_animation()
        {
            var obj = Object(new MaterialTexture { Transparency = TransparencyType.Filter });

            obj.Animation = new AnimationData
            {
                MaterialOpacity = [],
                TextureUv = new TextureUvAnimation[0, 4],
            };

            var state = RenderStateResolver.Resolve(obj, 0);

            Assert.That(state.UvAnimated, Is.False);
            Assert.That(state.OpacityAnimated, Is.False);
        }

        private static GeometryObject Object(MaterialTexture material)
        {
            return new GeometryObject { Materials = [material] };
        }

        private static GeometryObject AlphaTestedObject(uint func = 5, float opacity = 1f)
        {
            return Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                Opacity = opacity,
                RenderStates =
                [
                    new RenderStateAtom { State = 15, Value0 = 1 },
                    new RenderStateAtom { State = 25, Value0 = func },
                    new RenderStateAtom { State = 24, Value0 = 0 }
                ],
            });
        }
    }
}
