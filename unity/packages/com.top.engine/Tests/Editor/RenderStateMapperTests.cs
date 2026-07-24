using NUnit.Framework;
using Top.Assets.Conversion.Materials;
using Top.MindPower;
using Top.MindPower.Geometry;
using UnityEngine.Rendering;

namespace Top.Engine.Editor.Tests
{
    /// <summary>
    /// Resolving the render-state atoms is covered by
    /// Top.Assets.Conversion.Tests.RenderStateResolverTests. What is left
    /// here needs the engine: the D3D to Unity translation and everything
    /// downstream of it.
    /// </summary>
    public class RenderStateMapperTests
    {
        [Test]
        public void Translates_d3d_blend_and_cull()
        {
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.Zero), Is.EqualTo(BlendMode.Zero));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.One), Is.EqualTo(BlendMode.One));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.SrcColor), Is.EqualTo(BlendMode.SrcColor));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.InvSrcColor),
                Is.EqualTo(BlendMode.OneMinusSrcColor));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.SrcAlpha), Is.EqualTo(BlendMode.SrcAlpha));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.InvSrcAlpha),
                Is.EqualTo(BlendMode.OneMinusSrcAlpha));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.DestAlpha), Is.EqualTo(BlendMode.DstAlpha));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.InvDestAlpha),
                Is.EqualTo(BlendMode.OneMinusDstAlpha));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.DestColor), Is.EqualTo(BlendMode.DstColor));
            Assert.That(RenderStateMapper.ToUnity(D3DBlend.InvDestColor),
                Is.EqualTo(BlendMode.OneMinusDstColor));

            Assert.That(RenderStateMapper.ToUnity(D3DCull.None), Is.EqualTo(CullMode.Off));
            Assert.That(RenderStateMapper.ToUnity(D3DCull.Cw), Is.EqualTo(CullMode.Front));
            Assert.That(RenderStateMapper.ToUnity(D3DCull.Ccw), Is.EqualTo(CullMode.Back));
        }

        [Test]
        public void Render_queue_follows_blend_and_alpha_test()
        {
            Assert.That(Spec(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
            }).RenderQueue, Is.EqualTo(2000));

            Assert.That(Spec(new MaterialTexture
            {
                Transparency = TransparencyType.Filter,
                RenderStates = new[] { new RenderStateAtom { State = 15, Value0 = 1 } },
            }).RenderQueue, Is.EqualTo(2450));

            Assert.That(Spec(new MaterialTexture
            {
                Transparency = TransparencyType.Additive,
            }).RenderQueue, Is.EqualTo(3000));
        }

        [Test]
        public void Blended_materials_opt_out_of_shadows()
        {
            var blended = RenderStateMapper.CreateMaterial(Spec(
                new MaterialTexture { Transparency = TransparencyType.Additive }), null);
            Assert.That(blended.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF"), Is.True);
            Assert.That(blended.IsKeywordEnabled("_CAST_SHADOWS_OFF"), Is.True);

            var opaque = RenderStateMapper.CreateMaterial(Spec(
                new MaterialTexture { Transparency = TransparencyType.Filter }), null);
            Assert.That(opaque.IsKeywordEnabled("_RECEIVE_SHADOWS_OFF"), Is.False);
            Assert.That(opaque.IsKeywordEnabled("_CAST_SHADOWS_OFF"), Is.False);
        }

        [Test]
        public void Static_opacity_below_one_gets_the_opacity_treatment()
        {
            var material = RenderStateMapper.CreateMaterial(Spec(
                new MaterialTexture { Transparency = TransparencyType.Filter, Opacity = 0.99f }),
                null);

            Assert.That(material.GetFloat("_Opacity"), Is.EqualTo(0.99f));
            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo(5f), "SrcAlpha");
            Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo(10f), "OneMinusSrcAlpha");
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));
            Assert.That(material.renderQueue, Is.EqualTo(3000));

            var opaque = RenderStateMapper.CreateMaterial(Spec(
                new MaterialTexture { Transparency = TransparencyType.Filter }), null);

            Assert.That(opaque.GetFloat("_ZWrite"), Is.EqualTo(1f), "opacity 1 stays opaque");
            Assert.That(opaque.renderQueue, Is.EqualTo(2000));
        }

        [Test]
        public void Opacity_animation_forces_filter_blend_on_opaque_rows()
        {
            var material = RenderStateMapper.CreateMaterial(
                Spec(new MaterialTexture { Transparency = TransparencyType.Filter },
                    opacityAnimated: true), null);

            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo(5f), "SrcAlpha");
            Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo(10f), "OneMinusSrcAlpha");
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));
            Assert.That(material.renderQueue, Is.EqualTo(3000));
        }

        [Test]
        public void Opacity_animation_rewrites_additive_source_blend()
        {
            var material = RenderStateMapper.CreateMaterial(
                Spec(new MaterialTexture { Transparency = TransparencyType.Additive },
                    opacityAnimated: true), null);

            Assert.That(material.GetFloat("_SrcBlend"), Is.EqualTo(5f), "SrcAlpha");
            Assert.That(material.GetFloat("_DstBlend"), Is.EqualTo(1f), "One stays");
            Assert.That(material.GetFloat("_ZWrite"), Is.EqualTo(0f));

            var reserved = Spec(
                new MaterialTexture { Transparency = TransparencyType.Additive1 },
                opacityAnimated: true);

            Assert.That(RenderStateMapper.CreateMaterial(reserved, null).GetFloat("_SrcBlend"),
                Is.EqualTo(3f), "Additive1 keeps SrcColor; the engine's reserved branch");
        }

        [Test]
        public void Lighting_state_maps_to_the_unlit_toggle()
        {
            var unlit = RenderStateMapper.CreateMaterial(Spec(
                new MaterialTexture
                {
                    Transparency = TransparencyType.Filter,
                    RenderStates = new[] { new RenderStateAtom { State = 137, Value0 = 0 } },
                }), null);

            Assert.That(unlit.IsKeywordEnabled("_UNLIT_ON"), Is.True);
            Assert.That(unlit.GetFloat("_Unlit"), Is.EqualTo(1f));

            var lit = RenderStateMapper.CreateMaterial(Spec(
                new MaterialTexture { Transparency = TransparencyType.Filter }), null);

            Assert.That(lit.IsKeywordEnabled("_UNLIT_ON"), Is.False);
        }

        private static MaterialSpec Spec(MaterialTexture material, bool opacityAnimated = false)
        {
            var obj = new GeometryObject { Materials = new[] { material } };

            if (opacityAnimated)
            {
                obj.Animation = new AnimationData
                {
                    MaterialOpacity = new[]
                    {
                        new MaterialOpacityAnimation { Keys = new FloatKeyframe[2] },
                    },
                };
            }

            return RenderStateMapper.ToSpec(RenderStateResolver.Resolve(obj, 0));
        }
    }
}
