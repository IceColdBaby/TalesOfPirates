using System;
using System.IO;
using NUnit.Framework;
using Top.MindPower.Textures;

namespace Top.MindPower.Tests
{
    public class DdsReaderTests
    {
        [Test]
        public void Decodes_solid_red_dxt1_block()
        {
            var dds = BuildDxt1(4, 4, block =>
            {
                // c0 = c1 = pure red in RGB565; all indices 0.
                block[0] = 0x00;
                block[1] = 0xF8;
                block[2] = 0x00;
                block[3] = 0xF8;
            });

            var image = DdsReader.Read(dds);

            Assert.That(image.Width, Is.EqualTo(4));
            Assert.That(image.Height, Is.EqualTo(4));
            Assert.That(image.Pixels[0].R, Is.EqualTo(255));
            Assert.That(image.Pixels[0].G, Is.EqualTo(0));
            Assert.That(image.Pixels[0].B, Is.EqualTo(0));
            Assert.That(image.Pixels[0].A, Is.EqualTo(255));
        }

        [Test]
        public void Decodes_fixture_dds()
        {
            var image = DdsReader.Read(File.ReadAllBytes(Fixtures.Path("dds/010022.dds")));

            Assert.That(image.Width, Is.GreaterThan(0));
            Assert.That(image.Height, Is.GreaterThan(0));
            Assert.That(image.Pixels.Length, Is.EqualTo(image.Width * image.Height));
        }

        [Test]
        public void Tex_encode_decode_unswaps_header_and_strips_marker()
        {
            var expected = new byte[100];
            for (var i = 0; i < 100; i++)
            {
                expected[i] = (byte)i;
            }

            var payload = (byte[])expected.Clone();
            var tmp = new byte[44];
            Array.Copy(payload, tmp, 44);
            Array.Copy(payload, 100 - 44, payload, 0, 44);
            tmp.CopyTo(payload, 100 - 44);

            var encoded = new byte[104];
            payload.CopyTo(encoded, 0);
            encoded[100] = (byte)'m';
            encoded[101] = (byte)'p';
            encoded[102] = (byte)'.';
            encoded[103] = (byte)'x';

            Assert.That(TextureEncode.Decode(encoded), Is.EqualTo(expected));
        }

        [Test]
        public void Decodes_dxt1_four_color_interpolation()
        {
            var dds = BuildDxt1(4, 4, block =>
            {
                block[0] = 0x00;
                block[1] = 0xF8;
                block[2] = 0x1F;
                block[3] = 0x00;
                block[4] = 0xE4;
            });

            var image = DdsReader.Read(dds);

            Assert.That(image.Pixels[0], Is.EqualTo(new Rgba32(255, 0, 0, 255)));
            Assert.That(image.Pixels[1], Is.EqualTo(new Rgba32(0, 0, 255, 255)));
            Assert.That(image.Pixels[2], Is.EqualTo(new Rgba32(170, 0, 85, 255)));
            Assert.That(image.Pixels[3], Is.EqualTo(new Rgba32(85, 0, 170, 255)));
        }

        [Test]
        public void Decodes_dxt1_punch_through_alpha()
        {
            var dds = BuildDxt1(4, 4, block =>
            {
                block[0] = 0x1F;
                block[1] = 0x00;
                block[2] = 0x00;
                block[3] = 0xF8;
                block[4] = 0xE4;
            });

            var image = DdsReader.Read(dds);

            Assert.That(image.Pixels[0], Is.EqualTo(new Rgba32(0, 0, 255, 255)));
            Assert.That(image.Pixels[1], Is.EqualTo(new Rgba32(255, 0, 0, 255)));
            Assert.That(image.Pixels[2], Is.EqualTo(new Rgba32(127, 0, 127, 255)));
            Assert.That(image.Pixels[3], Is.EqualTo(new Rgba32(0, 0, 0, 0)));
        }

        private static byte[] BuildDxt1(int width, int height, Action<byte[]> fillBlock)
        {
            var header = new byte[128];
            BitConverter.GetBytes(0x20534444u).CopyTo(header, 0);
            BitConverter.GetBytes(124).CopyTo(header, 4);
            BitConverter.GetBytes(height).CopyTo(header, 12);
            BitConverter.GetBytes(width).CopyTo(header, 16);
            BitConverter.GetBytes(32).CopyTo(header, 76);
            BitConverter.GetBytes(0x4u).CopyTo(header, 80);
            BitConverter.GetBytes(0x31545844u).CopyTo(header, 84);
            var block = new byte[8];
            fillBlock(block);
            var result = new byte[128 + 8];
            header.CopyTo(result, 0);
            block.CopyTo(result, 128);
            return result;
        }
    }
}
