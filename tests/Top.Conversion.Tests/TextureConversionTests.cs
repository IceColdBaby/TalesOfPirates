using System.IO;
using NUnit.Framework;
using Top.Conversion.Textures;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.MindPower.Textures;

namespace Top.Conversion.Tests
{
    public class TextureConversionTests
    {
        private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

        [Test]
        public void Encodes_the_decoded_source_at_its_own_size()
        {
            // The fixture is obfuscated, so this also covers the decode the
            // readers do before sniffing the format.
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));
            var expected = TextureReader.Read(source);

            var png = TextureConversion.ToPng(source, new TextureStage());

            Assert.That(png[..8], Is.EqualTo(PngSignature));
            Assert.That(ReadInt32BE(png, 16), Is.EqualTo(expected.Width));
            Assert.That(ReadInt32BE(png, 20), Is.EqualTo(expected.Height));
        }

        [Test]
        public void Color_keyed_stages_encode_differently()
        {
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));
            var first = TextureReader.Read(source).Pixels[0];

            var plain = TextureConversion.ToPng(source, new TextureStage());
            var keyed = TextureConversion.ToPng(source, new TextureStage
            {
                ColorKeyType = ColorKeyType.Specific,
                ColorKey = first,
            });

            Assert.That(keyed, Is.Not.EqualTo(plain),
                "the stage's color key reaches the encoded output");
        }

        [Test]
        public void First_pixel_keying_matches_keying_on_that_pixel()
        {
            var source = File.ReadAllBytes(Fixtures.Path("dds/010022.dds"));
            var first = TextureReader.Read(source).Pixels[0];

            var byValue = TextureConversion.ToPng(source, new TextureStage
            {
                ColorKeyType = ColorKeyType.Specific,
                ColorKey = first,
            });
            var byPosition = TextureConversion.ToPng(source, new TextureStage
            {
                ColorKeyType = ColorKeyType.FirstPixel,
            });

            Assert.That(byPosition, Is.EqualTo(byValue));
        }

        [Test]
        public void Undecodable_sources_throw()
        {
            var source = File.ReadAllBytes(Fixtures.Path("obj/teampk.obj"));

            Assert.That(() => TextureConversion.ToPng(source, new TextureStage()),
                Throws.InstanceOf<InvalidDataException>());
        }

        private static int ReadInt32BE(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }
    }
}
