using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Assets.Contract.Models;
using Top.Assets.Contract.Models.Extras;
using Top.Assets.Contract.Models.Materials;
using Top.Assets.Conversion.Models;
using Top.Assets.Conversion.Models.Gltf;
using Top.Assets.Conversion.Models.Materials;
using Top.Gltf;
using Top.MindPower;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
{
    public class MaterialWriterTests
    {
        private static GeometryObject Object(AnimationData animation = null)
        {
            return new GeometryObject
            {
                Materials = [new MaterialTexture()],
                Animation = animation,
            };
        }

        private static MaterialExtras Map(GeometryObject obj, RenderState state = null)
        {
            return MaterialWriter.Extras(obj, 0, state ?? new RenderState(), _ => 0);
        }

        private static void AssertSurvivesTheFile(RenderState state)
        {
            var builder = new GltfBuilder("scene");

            MaterialWriter.Extras(Object(), 0, state, _ => 0).Write(builder.AddMaterial("mat"));

            var file = builder.Build();

            using var stream = new MemoryStream();
            GltfWriter.WriteGlb(file.Document, file.BinChunk, stream);
            stream.Position = 0;

            MaterialExtras.TryRead(GltfReader.Read(stream).Document.Materials[0], out var extras);
            var read = extras.RenderState;

            Assert.That(read.SrcBlend, Is.EqualTo(state.SrcBlend));
            Assert.That(read.DstBlend, Is.EqualTo(state.DstBlend));
            Assert.That(read.BlendEnabled, Is.EqualTo(state.BlendEnabled));
            Assert.That(read.ZWrite, Is.EqualTo(state.ZWrite));
            Assert.That(read.Lit, Is.EqualTo(state.Lit));
            Assert.That(read.Transparency, Is.EqualTo(state.Transparency));
        }

        [Test]
        public void A_vanilla_render_state_survives_being_left_out_of_the_file()
        {
            AssertSurvivesTheFile(new RenderState());
        }

        [Test]
        public void A_render_state_off_every_default_survives_the_file()
        {
            AssertSurvivesTheFile(new RenderState
            {
                SrcBlend = BlendFactor.DstAlpha,
                DstBlend = BlendFactor.OneMinusDstColor,
                BlendEnabled = true,
                ZWrite = false,
                Lit = false,
                Transparency = TransparencyMode.Subtractive,
            });
        }

        [Test]
        public void Render_state_carries_what_the_standard_fields_cannot()
        {
            var extras = Map(Object(), new RenderState
            {
                SrcBlend = BlendFactor.SrcColor,
                DstBlend = BlendFactor.One,
                BlendEnabled = true,
                ZWrite = false,
                Lit = false,
                Transparency = TransparencyMode.Additive1,
            });

            Assert.That(extras.RenderState.SrcBlend, Is.EqualTo(BlendFactor.SrcColor));
            Assert.That(extras.RenderState.DstBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(extras.RenderState.BlendEnabled, Is.True);
            Assert.That(extras.RenderState.ZWrite, Is.False);
            Assert.That(extras.RenderState.Lit, Is.False);
            Assert.That(extras.RenderState.Transparency, Is.EqualTo(TransparencyMode.Additive1));
        }

        [Test]
        public void Only_front_culling_needs_naming()
        {
            Assert.That(Map(Object(), new RenderState { Cull = FaceCulling.Front })
                .RenderState.Cull, Is.EqualTo(FaceCulling.Front));

            Assert.That(Map(Object(), new RenderState { Cull = FaceCulling.Back })
                .RenderState.Cull, Is.Null, "the standard default already culls the back");

            Assert.That(Map(Object(), new RenderState { Cull = FaceCulling.None })
                .RenderState.Cull, Is.Null, "double sidedness says this one");
        }

        [Test]
        public void A_material_that_blends_and_alpha_tests_keeps_its_cutoff()
        {
            var extras = Map(Object(), new RenderState
            {
                BlendEnabled = true,
                AlphaTest = true,
                Cutoff = 0.4f,
            });

            Assert.That(extras.RenderState.AlphaTestCutoff, Is.EqualTo(0.4f));
        }

        [Test]
        public void An_alpha_tested_material_that_stays_opaque_leaves_the_cutoff_standard()
        {
            var extras = Map(Object(), new RenderState { AlphaTest = true, Cutoff = 0.4f });

            Assert.That(extras.RenderState.AlphaTestCutoff, Is.Null);
        }

        [Test]
        public void Uv_frames_carry_the_six_cells_the_transform_uses()
        {
            var frame = new Matrix4x4(
                11, 12, 13, 14,
                21, 22, 23, 24,
                31, 32, 33, 34,
                41, 42, 43, 44);
            var uv = new TextureUvAnimation[1, 4];
            uv[0, 0] = new TextureUvAnimation { Frames = [frame] };

            var extras = Map(Object(new AnimationData { TextureUv = uv }));

            Assert.That(extras.UvAnimation.Frames,
                Is.EqualTo(new[] { new float[] { 11, 12, 21, 22, 31, 32 } }));
            Assert.That(extras.UvAnimation.FramesPerSecond,
                Is.EqualTo(AnimationRate.FramesPerSecond));
        }

        [Test]
        public void Opacity_keys_carry_their_frame_and_value()
        {
            var extras = Map(Object(new AnimationData
            {
                MaterialOpacity =
                [
                    new MaterialOpacityAnimation
                    {
                        Keys =
                        [
                            new FloatKeyframe { Key = 0, Value = 1f },
                            new FloatKeyframe { Key = 20, Value = 0.25f }
                        ],
                    }
                ],
            }));

            Assert.That(extras.OpacityAnimation.KeyFrames, Is.EqualTo(new[] { 0, 20 }));
            Assert.That(extras.OpacityAnimation.Values, Is.EqualTo(new[] { 1f, 0.25f }));
        }

        [Test]
        public void Flipbook_frames_point_at_gltf_textures()
        {
            var flipbook = new TextureImageAnimation[1, 4];
            flipbook[0, 0] = new TextureImageAnimation
            {
                DataSequence =
                [
                    new TextureStage { FileName = "a.bmp" },
                    new TextureStage(),
                    new TextureStage { FileName = "b.bmp" }
                ],
            };

            var extras = MaterialWriter.Extras(
                Object(new AnimationData { TextureImage = flipbook }), 0,
                new RenderState(),
                file => file == "a.bmp" ? 7 : 9);

            Assert.That(extras.Flipbook.Frames,
                Is.EqualTo(new[] { 7, FlipbookExtras.NoTexture, 9 }));
        }

        [Test]
        public void Tracks_on_another_stage_never_reach_the_material()
        {
            var uv = new TextureUvAnimation[1, 4];
            uv[0, 1] = new TextureUvAnimation { Frames = new Matrix4x4[2] };
            var flipbook = new TextureImageAnimation[1, 4];
            flipbook[0, 2] = new TextureImageAnimation
            {
                DataSequence = [new TextureStage { FileName = "a.bmp" }],
            };

            var extras = Map(Object(new AnimationData { TextureUv = uv, TextureImage = flipbook }));

            Assert.That(extras.UvAnimation, Is.Null);
            Assert.That(extras.Flipbook, Is.Null);
        }

        [Test]
        public void A_material_past_the_end_of_the_track_grid_has_no_tracks()
        {
            var uv = new TextureUvAnimation[1, 4];
            uv[0, 0] = new TextureUvAnimation { Frames = new Matrix4x4[2] };

            var extras = MaterialWriter.Extras(
                Object(new AnimationData
                {
                    TextureUv = uv,
                    MaterialOpacity =
                    [
                        new MaterialOpacityAnimation { Keys = new FloatKeyframe[2] }
                    ],
                }),
                1, new RenderState(), _ => 0);

            Assert.That(extras.UvAnimation, Is.Null);
            Assert.That(extras.OpacityAnimation, Is.Null);
        }

        [Test]
        public void A_material_without_tracks_carries_no_animation_section()
        {
            var extras = Map(Object());

            Assert.That(extras.RenderState, Is.Not.Null);
            Assert.That(extras.UvAnimation, Is.Null);
            Assert.That(extras.OpacityAnimation, Is.Null);
            Assert.That(extras.Flipbook, Is.Null);
        }

        [Test]
        public void Empty_tracks_read_as_no_track_at_all()
        {
            var uv = new TextureUvAnimation[1, 4];
            uv[0, 0] = new TextureUvAnimation { Frames = [] };
            var flipbook = new TextureImageAnimation[1, 4];
            flipbook[0, 0] = new TextureImageAnimation { DataSequence = [] };

            var extras = Map(Object(new AnimationData
            {
                TextureUv = uv,
                TextureImage = flipbook,
                MaterialOpacity = [new MaterialOpacityAnimation { Keys = [] }],
            }));

            Assert.That(extras.UvAnimation, Is.Null);
            Assert.That(extras.OpacityAnimation, Is.Null);
            Assert.That(extras.Flipbook, Is.Null);
        }

        [Test]
        public void Every_emitted_material_carries_its_payload_and_nothing_else_does()
        {
            using var stream = File.OpenRead(Fixtures.Path("lmo/by-bd015.lmo"));
            var doc = GltfExport.Model("by-bd015", LmoFile.Read(stream).Model, "../Textures")
                .Document;

            Assert.That(doc.Materials, Is.Not.Empty);

            foreach (var material in doc.Materials)
            {
                Assert.That(MaterialExtras.TryRead(material, out _), Is.True, material.Name);
            }

            Assert.That(doc.Nodes.TrueForAll(node => node.Extras == null));
            Assert.That(doc.Meshes.TrueForAll(mesh => mesh.Extras == null));
            Assert.That(doc.Textures.TrueForAll(texture => texture.Extras == null));
            Assert.That(doc.Extras, Is.Null);
        }

        [Test]
        public void Flipbook_frames_index_textures_the_document_holds()
        {
            var obj = ModelPackagingTests.MakeTexturedObject("1.BMP");
            var flipbook = new TextureImageAnimation[1, 4];
            flipbook[0, 0] = new TextureImageAnimation
            {
                DataSequence =
                [
                    new TextureStage { FileName = "pstone01.BMP" },
                    new TextureStage { FileName = "1.BMP" },
                ],
            };
            obj.Animation = new AnimationData { TextureImage = flipbook };

            var doc = GltfExport.Object("flipbook", obj, "../Textures").Document;
            MaterialExtras.TryRead(doc.Materials[0], out var extras);

            var uris = extras.Flipbook.Frames
                .Select(frame => doc.Images[doc.Textures[frame].Source.Value].Uri);

            Assert.That(uris, Is.EqualTo(new[] { "../Textures/pstone01.png", "../Textures/1.png" }));
            Assert.That(extras.Flipbook.Frames[1],
                Is.EqualTo(doc.Materials[0].PbrMetallicRoughness.BaseColorTexture.Index),
                "a frame naming the material's own texture reuses it");
        }

        [Test]
        public void A_gltf_with_no_materials_carries_no_extras()
        {
            using var stream = File.OpenRead(Fixtures.Path("lmo/by-bd015.lmo"));
            var file = GltfExport.Model("by-bd015", LmoFile.Read(stream).Model);

            Assert.That(GltfJson.Serialize(file.Document, indented: false),
                Does.Not.Contain("\"extras\""));
        }
    }
}
