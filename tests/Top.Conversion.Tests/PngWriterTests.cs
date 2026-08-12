using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using Top.Conversion.Textures;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Textures;

namespace Top.Conversion.Tests
{
    public class PngWriterTests
    {
        [Test]
        public void Writes_decodable_rgba_png()
        {
            var image = new RgbaImage(2, 2, [
                new Rgba32(255, 0, 0, 255), new Rgba32(0, 255, 0, 128),
                new Rgba32(0, 0, 255, 255), new Rgba32(255, 0, 255, 0)
            ]);

            var png = PngWriter.Write(image);

            Assert.That(png[0], Is.EqualTo(137));
            Assert.That(png[1], Is.EqualTo((byte)'P'));

            // IHDR payload starts at offset 16.
            Assert.That(ReadInt32BE(png, 16), Is.EqualTo(2), "width");
            Assert.That(ReadInt32BE(png, 20), Is.EqualTo(2), "height");
            Assert.That(png[24], Is.EqualTo(8), "bit depth");
            Assert.That(png[25], Is.EqualTo(6), "color type RGBA");

            // Inflate IDAT and check the first scanline: filter 0 + RGBA pixels.
            var idatOffset = FindChunk(png, "IDAT");
            var idatLength = ReadInt32BE(png, idatOffset - 8);
            using var deflated = new MemoryStream(png, idatOffset + 2, idatLength - 6);
            using var inflate = new DeflateStream(deflated, CompressionMode.Decompress);
            using var raw = new MemoryStream();
            inflate.CopyTo(raw);
            var scanlines = raw.ToArray();

            Assert.That(scanlines.Length, Is.EqualTo((2 * 4 + 1) * 2));
            Assert.That(scanlines[0], Is.EqualTo(0), "filter byte");
            Assert.That(scanlines[1], Is.EqualTo(255), "r");
            Assert.That(scanlines[4], Is.EqualTo(255), "a");
        }

        private static int ReadInt32BE(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16)
                | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static int FindChunk(byte[] png, string type)
        {
            for (var i = 8; i < png.Length - 4; i++)
            {
                if (png[i] == type[0] && png[i + 1] == type[1]
                    && png[i + 2] == type[2] && png[i + 3] == type[3])
                {
                    return i + 4;
                }
            }

            throw new InvalidDataException($"chunk {type} not found");
        }
    }
}
