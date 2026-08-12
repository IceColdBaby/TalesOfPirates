using System.IO;
using NUnit.Framework;
using Top.Legacy.MindPower.Textures;

namespace Top.Legacy.MindPower.Tests
{
    public class TextureReaderTests
    {
        private static RgbaImage Read(string relative)
        {
            return TextureReader.Read(File.ReadAllBytes(Fixtures.Path(relative)));
        }

        private static void AssertProbes(RgbaImage image, int width, int height,
            (byte R, byte G, byte B, byte A) first,
            (byte R, byte G, byte B, byte A) middle,
            (byte R, byte G, byte B, byte A) last)
        {
            Assert.That((image.Width, image.Height), Is.EqualTo((width, height)));
            Assert.That(Probe(image.Pixels[0]), Is.EqualTo(first), "top-left");
            Assert.That(Probe(image.Pixels[((height / 2) * width) + (width / 2)]),
                Is.EqualTo(middle), "center");
            Assert.That(Probe(image.Pixels[(width * height) - 1]), Is.EqualTo(last),
                "bottom-right");
        }

        private static (byte, byte, byte, byte) Probe(Rgba32 pixel)
        {
            return (pixel.R, pixel.G, pixel.B, pixel.A);
        }

        [Test]
        public void Bmp_8bit_palettized()
        {
            AssertProbes(Read("bmp/0213000001.bmp"), 16, 16,
                (0, 0, 0, 255), (255, 0, 0, 255), (0, 0, 0, 255));
        }

        [Test]
        public void Bmp_16bit()
        {
            AssertProbes(Read("bmp/5000000017.BMP"), 64, 64,
                (231, 231, 231, 255), (74, 74, 74, 255), (189, 189, 189, 255));
        }

        [Test]
        public void Bmp_24bit()
        {
            AssertProbes(Read("bmp/1.BMP"), 32, 32,
                (16, 117, 234, 255), (104, 209, 240, 255), (16, 117, 234, 255));
        }

        [Test]
        public void Bmp_32bit_is_opaque()
        {
            AssertProbes(Read("bmp/pstone01.BMP"), 64, 64,
                (249, 255, 221, 255), (255, 234, 211, 255), (30, 3, 0, 255));
        }

        [Test]
        public void Bmp_32bit_bitfields()
        {
            AssertProbes(Read("bmp/BATMAN0023.bmp"), 64, 64,
                (36, 36, 36, 255), (15, 15, 15, 255), (36, 36, 36, 255));
        }

        [Test]
        public void Tga_truecolor_24bit()
        {
            AssertProbes(Read("tga/9011001404.tga"), 64, 64,
                (255, 255, 255, 255), (109, 118, 128, 255), (91, 101, 114, 255));
        }

        [Test]
        public void Tga_truecolor_32bit()
        {
            AssertProbes(Read("tga/0000254790.tga"), 64, 64,
                (47, 31, 32, 255), (20, 17, 37, 255), (48, 48, 48, 255));
        }

        [Test]
        public void Tga_colormapped()
        {
            AssertProbes(Read("tga/9011001903.tga"), 64, 64,
                (238, 165, 106, 255), (211, 128, 77, 255), (241, 171, 111, 255));
        }

        [Test]
        public void Tga_rle_truecolor_32bit_keeps_alpha()
        {
            AssertProbes(Read("tga/03040032.tga"), 128, 128,
                (255, 255, 255, 0), (255, 255, 255, 0), (255, 255, 255, 0));
        }

        [Test]
        public void Tga_rle_truecolor_24bit()
        {
            AssertProbes(Read("tga/9013003701.tga"), 128, 128,
                (19, 26, 12, 255), (14, 12, 16, 255), (19, 26, 12, 255));
        }

        [Test]
        public void Tga_rle_colormapped()
        {
            AssertProbes(Read("tga/NATSU00001.tga"), 128, 128,
                (209, 113, 152, 255), (209, 113, 152, 255), (221, 124, 164, 255));
        }

        [Test]
        public void Dds_is_detected_by_magic()
        {
            var image = Read("dds/010022.dds");

            Assert.That(image.Width, Is.GreaterThan(0));
            Assert.That(image.Pixels, Has.Length.EqualTo(image.Width * image.Height));
        }

        [Test]
        public void Garbage_is_rejected()
        {
            var bytes = new byte[32];
            bytes[2] = 77;

            Assert.That(() => TextureReader.Read(bytes),
                Throws.TypeOf<InvalidDataException>());
        }
    }
}
