using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Top.Contracts.Assets.Models;
using Top.Contracts.Assets.Models.Extras;
using Top.Contracts.Assets.Models.Materials;
using Top.Conversion.Gltf;

namespace Top.Conversion.Tests
{
    /// <summary>
    /// The payload against a real glTF file rather than a bare JSON object:
    /// what conversion writes is what a reader gets back.
    /// </summary>
    public class MaterialExtrasFileTests
    {
        private static MaterialExtras Reread(MaterialExtras extras)
        {
            var builder = new GltfBuilder("scene");

            builder.AddMaterial("mat").WithExtras(new JObject { [MaterialExtras.Key] = extras.ToJson() });

            var file = builder.Build();

            using var stream = new MemoryStream();
            GltfWriter.WriteGlb(file.Document, file.BinChunk, stream);
            stream.Position = 0;

            var material = GltfReader.Read(stream).Document.Materials[0];

            Assert.That(MaterialExtras.TryRead(material.Extras as JObject, out var read), Is.True);

            return read;
        }

        [Test]
        public void Every_section_survives_a_written_and_reread_file()
        {
            var read = Reread(new MaterialExtras
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
            });

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

            Assert.That(read.OpacityAnimation.FramesPerSecond, Is.EqualTo(AnimationRate.FramesPerSecond));
            Assert.That(read.OpacityAnimation.KeyFrames, Is.EqualTo(new[] { 0, 12, 30 }));
            Assert.That(read.OpacityAnimation.Values, Is.EqualTo(new[] { 1f, 0.5f, 0f }));

            Assert.That(read.Flipbook.Frames, Is.EqualTo(new[] { 3, -1, 4 }));
        }
    }
}
