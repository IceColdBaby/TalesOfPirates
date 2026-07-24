using System.Numerics;
using NUnit.Framework;
using Top.Assets.Conversion.Materials;
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

            Assert.That(state.SrcBlend, Is.EqualTo(D3DBlend.One));
            Assert.That(state.DstBlend, Is.EqualTo(D3DBlend.One));
            Assert.That(state.BlendEnabled, Is.True);
        }

        [Test]
        public void Alpha_test_ignores_the_exported_ref()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[]
                {
                    new RenderStateAtom { State = 15, Value0 = 1 },
                    new RenderStateAtom { State = 25, Value0 = 5 },
                    new RenderStateAtom { State = 24, Value0 = 0 },
                },
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
                RenderStates = new[]
                {
                    new RenderStateAtom { State = 14, Value0 = 0 },
                    new RenderStateAtom { State = 22, Value0 = 1 },
                },
            }), 0);

            Assert.That(state.ZWrite, Is.False);
            Assert.That(state.Cull, Is.EqualTo(D3DCull.None));
        }

        [Test]
        public void Unknown_states_produce_warnings()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[] { new RenderStateAtom { State = 999, Value0 = 1 } },
            }), 0);

            Assert.That(state.Warnings, Has.Some.Contains("999"));
        }

        [Test]
        public void Transparency_type_overrides_blend_factor_atoms()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive,
                RenderStates = new[]
                {
                    new RenderStateAtom { State = 19, Value0 = 5 },
                    new RenderStateAtom { State = 20, Value0 = 6 },
                },
            }), 0);

            Assert.That(state.SrcBlend, Is.EqualTo(D3DBlend.One));
            Assert.That(state.DstBlend, Is.EqualTo(D3DBlend.One));
            Assert.That(state.BlendEnabled, Is.True);
        }

        [Test]
        public void Filter_keeps_blend_factor_atoms()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[]
                {
                    new RenderStateAtom { State = 19, Value0 = 5 },
                    new RenderStateAtom { State = 20, Value0 = 6 },
                    new RenderStateAtom { State = 27, Value0 = 1 },
                },
            }), 0);

            Assert.That(state.SrcBlend, Is.EqualTo(D3DBlend.SrcAlpha));
            Assert.That(state.DstBlend, Is.EqualTo(D3DBlend.InvSrcAlpha));
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
            Assert.That(additive1.SrcBlend, Is.EqualTo(D3DBlend.SrcColor));
            Assert.That(additive1.DstBlend, Is.EqualTo(D3DBlend.One));

            var additive2 = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive2,
            }), 0);
            Assert.That(additive2.SrcBlend, Is.EqualTo(D3DBlend.SrcColor));
            Assert.That(additive2.DstBlend, Is.EqualTo(D3DBlend.InvSrcColor));

            var additive3 = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Additive3,
            }), 0);
            Assert.That(additive3.SrcBlend, Is.EqualTo(D3DBlend.SrcAlpha));
            Assert.That(additive3.DstBlend, Is.EqualTo(D3DBlend.DestAlpha));

            var subtractive = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Subtractive,
            }), 0);
            Assert.That(subtractive.SrcBlend, Is.EqualTo(D3DBlend.Zero));
            Assert.That(subtractive.DstBlend, Is.EqualTo(D3DBlend.InvSrcColor));

            var subtractive1 = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Subtractive1,
            }), 0);
            Assert.That(subtractive1.SrcBlend, Is.EqualTo(D3DBlend.One));
            Assert.That(subtractive1.DstBlend, Is.EqualTo(D3DBlend.One));
            Assert.That(subtractive1.Warnings, Is.Not.Empty);
        }

        [Test]
        public void Ignored_and_terminator_atoms_produce_no_warnings()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[]
                {
                    new RenderStateAtom { State = 23, Value0 = 4 },
                    new RenderStateAtom { State = 26, Value0 = 0 },
                    new RenderStateAtom { State = 28, Value0 = 0 },
                    new RenderStateAtom { State = 137, Value0 = 0 },
                    new RenderStateAtom { State = 0xFFFFFFFF, Value0 = 0 },
                },
            }), 0);

            Assert.That(state.Warnings, Is.Empty);
        }

        [Test]
        public void Cull_modes_resolve_cw_and_ccw()
        {
            Assert.That(RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[] { new RenderStateAtom { State = 22, Value0 = 2 } },
            }), 0).Cull, Is.EqualTo(D3DCull.Cw));

            Assert.That(RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[] { new RenderStateAtom { State = 22, Value0 = 3 } },
            }), 0).Cull, Is.EqualTo(D3DCull.Ccw));
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
        public void Greater_alpha_func_does_not_warn()
        {
            Assert.That(RenderStateResolver.Resolve(AlphaTestedObject(func: 5), 0).Warnings, Is.Empty);
        }

        [Test]
        public void Other_alpha_funcs_warn()
        {
            Assert.That(RenderStateResolver.Resolve(AlphaTestedObject(func: 7), 0).Warnings,
                Has.Some.Contains("alpha func 7"));
            Assert.That(RenderStateResolver.Resolve(AlphaTestedObject(func: 4), 0).Warnings,
                Has.Some.Contains("alpha func 4"),
                "the reader replaces the legacy func, so it never reaches here");
        }

        [Test]
        public void Alpha_func_is_ignored_without_alpha_test()
        {
            var state = RenderStateResolver.Resolve(Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[] { new RenderStateAtom { State = 25, Value0 = 7 } },
            }), 0);

            Assert.That(state.Warnings, Is.Empty);
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
                MaterialOpacity = new[]
                {
                    new MaterialOpacityAnimation { Keys = new FloatKeyframe[2] },
                },
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
                MaterialOpacity = new MaterialOpacityAnimation[0],
                TextureUv = new TextureUvAnimation[0, 4],
            };

            var state = RenderStateResolver.Resolve(obj, 0);

            Assert.That(state.UvAnimated, Is.False);
            Assert.That(state.OpacityAnimated, Is.False);
        }

        private static GeometryObject Object(MaterialTexture material)
        {
            return new GeometryObject { Materials = new[] { material } };
        }

        private static GeometryObject AlphaTestedObject(uint func = 5, float opacity = 1f)
        {
            return Object(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                Opacity = opacity,
                RenderStates = new[]
                {
                    new RenderStateAtom { State = 15, Value0 = 1 },
                    new RenderStateAtom { State = 25, Value0 = func },
                    new RenderStateAtom { State = 24, Value0 = 0 },
                },
            });
        }
    }
}
