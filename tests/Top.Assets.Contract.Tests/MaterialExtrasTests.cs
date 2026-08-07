using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Assets.Contract.Models;
using Top.Assets.Contract.Models.Extras;
using Top.Assets.Contract.Models.Materials;
using Top.Gltf;

namespace Top.Assets.Contract.Tests
{
    public class MaterialExtrasTests
    {
        private static GltfMaterial Written(MaterialExtras extras)
        {
            var builder = new GltfBuilder("scene");

            extras.Write(builder.AddMaterial("mat"));

            return builder.Document.Materials[0];
        }

        private static GltfMaterial Reread(MaterialExtras extras)
        {
            var builder = new GltfBuilder("scene");

            extras.Write(builder.AddMaterial("mat"));

            var file = builder.Build();

            using var stream = new MemoryStream();
            GltfWriter.WriteGlb(file.Document, file.BinChunk, stream);
            stream.Position = 0;

            return GltfReader.Read(stream).Document.Materials[0];
        }

        [Test]
        public void Every_section_survives_a_written_and_reread_file()
        {
            var written = new MaterialExtras
            {
                RenderState = new RenderStateExtras
                {
                    SrcBlend = BlendFactor.SrcColor,
                    DstBlend = BlendFactor.OneMinusSrcColor,
                    BlendEnabled = true,
                    ZWrite = false,
                    Cull = FaceCulling.Front,
                    AlphaTestCutoff = 0.25f,
                    Lit = false,
                    Transparency = TransparencyMode.Additive2,
                },
                UvAnimation = new UvAnimationExtras
                {
                    FramesPerSecond = 15f,
                    Frames = [[1f, 0f, 0f, 1f, 0.5f, 0.25f], [2f, 0f, 0f, 2f, 0f, 0f]],
                },
                OpacityAnimation = new OpacityAnimationExtras
                {
                    KeyFrames = [0, 12, 30],
                    Values = [1f, 0.5f, 0f],
                },
                Flipbook = new FlipbookExtras
                {
                    Frames = [3, FlipbookExtras.NoTexture, 4],
                },
            };

            Assert.That(MaterialExtras.TryRead(Reread(written), out var read), Is.True);

            Assert.That(read.RenderState.SrcBlend, Is.EqualTo(BlendFactor.SrcColor));
            Assert.That(read.RenderState.DstBlend, Is.EqualTo(BlendFactor.OneMinusSrcColor));
            Assert.That(read.RenderState.BlendEnabled, Is.True);
            Assert.That(read.RenderState.ZWrite, Is.False);
            Assert.That(read.RenderState.Cull, Is.EqualTo(FaceCulling.Front));
            Assert.That(read.RenderState.AlphaTestCutoff, Is.EqualTo(0.25f));
            Assert.That(read.RenderState.Lit, Is.False);
            Assert.That(read.RenderState.Transparency, Is.EqualTo(TransparencyMode.Additive2));

            Assert.That(read.UvAnimation.FramesPerSecond, Is.EqualTo(15f));
            Assert.That(read.UvAnimation.Frames[0], Is.EqualTo(new[] { 1f, 0f, 0f, 1f, 0.5f, 0.25f }));
            Assert.That(read.UvAnimation.Frames[1], Is.EqualTo(new[] { 2f, 0f, 0f, 2f, 0f, 0f }));

            Assert.That(read.OpacityAnimation.FramesPerSecond,
                Is.EqualTo(AnimationRate.FramesPerSecond));
            Assert.That(read.OpacityAnimation.KeyFrames, Is.EqualTo(new[] { 0, 12, 30 }));
            Assert.That(read.OpacityAnimation.Values, Is.EqualTo(new[] { 1f, 0.5f, 0f }));

            Assert.That(read.Flipbook.Frames, Is.EqualTo(new[] { 3, -1, 4 }));
        }

        [Test]
        public void A_material_carrying_no_extras_reads_as_absent()
        {
            Assert.That(MaterialExtras.TryRead(new GltfMaterial(), out var extras), Is.False);
            Assert.That(extras, Is.Null);
        }

        [Test]
        public void Extras_from_another_tool_are_not_a_TOP_material()
        {
            var material = new GltfMaterial { Extras = new JObject { ["blender"] = "custom" } };

            Assert.That(MaterialExtras.TryRead(material, out _), Is.False);
        }

        [Test]
        public void An_unreadable_payload_reads_as_absent_rather_than_throwing()
        {
            var material = new GltfMaterial
            {
                Extras = new JObject { [MaterialExtras.Key] = "not an object" },
            };

            Assert.That(MaterialExtras.TryRead(material, out _), Is.False);
        }

        [Test]
        public void An_empty_payload_leaves_every_section_absent()
        {
            var material = new GltfMaterial
            {
                Extras = new JObject { [MaterialExtras.Key] = new JObject() },
            };

            Assert.That(MaterialExtras.TryRead(material, out var extras), Is.True);
            Assert.That(extras.RenderState, Is.Null);
            Assert.That(extras.UvAnimation, Is.Null);
            Assert.That(extras.OpacityAnimation, Is.Null);
            Assert.That(extras.Flipbook, Is.Null);
        }

        [Test]
        public void An_absent_field_resolves_to_the_vanilla_default()
        {
            var material = new GltfMaterial
            {
                Extras = new JObject
                {
                    [MaterialExtras.Key] = new JObject { ["renderState"] = new JObject() },
                },
            };

            MaterialExtras.TryRead(material, out var extras);
            var state = extras.RenderState;

            Assert.That(state.SrcBlend, Is.EqualTo(BlendFactor.One));
            Assert.That(state.DstBlend, Is.EqualTo(BlendFactor.Zero));
            Assert.That(state.BlendEnabled, Is.False);
            Assert.That(state.ZWrite, Is.True);
            Assert.That(state.Cull, Is.Null);
            Assert.That(state.AlphaTestCutoff, Is.Null);
            Assert.That(state.Lit, Is.True);
            Assert.That(state.Transparency, Is.EqualTo(TransparencyMode.Filter));
        }

        [Test]
        public void Fields_left_at_their_default_stay_out_of_the_file()
        {
            var material = Written(new MaterialExtras { RenderState = new RenderStateExtras() });
            var payload = (JObject)material.Extras[MaterialExtras.Key];

            Assert.That(((JObject)payload["renderState"]).Count, Is.Zero);
            Assert.That(payload["uvAnimation"], Is.Null);
            Assert.That(payload["opacityAnimation"], Is.Null);
            Assert.That(payload["flipbook"], Is.Null);
        }

        [Test]
        public void The_payload_hangs_off_its_own_member_of_the_extras_object()
        {
            var material = Written(new MaterialExtras());

            Assert.That(material.Extras, Is.InstanceOf<JObject>());
            Assert.That(material.Extras[MaterialExtras.Key], Is.InstanceOf<JObject>());
        }

        [Test]
        public void Enums_read_back_by_name_so_the_vocabulary_is_the_file_format()
        {
            var material = Written(new MaterialExtras
            {
                RenderState = new RenderStateExtras { SrcBlend = BlendFactor.DstColor },
            });
            var state = material.Extras[MaterialExtras.Key]["renderState"];

            Assert.That(state["srcBlend"].Value<string>(), Is.EqualTo("DstColor"));
        }
    }
}
