using System.IO;
using System.IO.Compression;
using Top.Legacy.MindPower.Textures;

namespace Top.Conversion.Textures
{
    /// <summary>
    /// Minimal PNG encoder: 8-bit RGBA, filter 0, single IDAT.
    /// </summary>
    public static class PngWriter
    {
        private static readonly uint[] CrcTable = BuildCrcTable();

        public static byte[] Write(RgbaImage image)
        {
            using var output = new MemoryStream();
            output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);

            var ihdr = new byte[13];
            WriteInt32BE(ihdr, 0, image.Width);
            WriteInt32BE(ihdr, 4, image.Height);
            ihdr[8] = 8;
            ihdr[9] = 6;
            WriteChunk(output, "IHDR", ihdr);

            var stride = (image.Width * 4) + 1;
            var raw = new byte[stride * image.Height];

            for (var y = 0; y < image.Height; y++)
            {
                var row = y * stride;
                raw[row] = 0;

                for (var x = 0; x < image.Width; x++)
                {
                    var p = image.Pixels[(y * image.Width) + x];
                    var o = row + 1 + (x * 4);
                    raw[o + 0] = p.R;
                    raw[o + 1] = p.G;
                    raw[o + 2] = p.B;
                    raw[o + 3] = p.A;
                }
            }

            WriteChunk(output, "IDAT", Zlib(raw));
            WriteChunk(output, "IEND", System.Array.Empty<byte>());

            return output.ToArray();
        }

        private static byte[] Zlib(byte[] raw)
        {
            using var ms = new MemoryStream();
            ms.WriteByte(0x78);
            ms.WriteByte(0x9C);

            using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                deflate.Write(raw, 0, raw.Length);
            }

            uint a = 1, b = 0;

            foreach (var value in raw)
            {
                a = (a + value) % 65521;
                b = (b + a) % 65521;
            }

            var adler = new byte[4];

            WriteInt32BE(adler, 0, (int)((b << 16) | a));
            ms.Write(adler, 0, 4);

            return ms.ToArray();
        }

        private static void WriteChunk(Stream stream, string type, byte[] payload)
        {
            var header = new byte[8];

            WriteInt32BE(header, 0, payload.Length);

            for (var i = 0; i < 4; i++)
            {
                header[4 + i] = (byte)type[i];
            }

            stream.Write(header, 0, 8);
            stream.Write(payload, 0, payload.Length);

            var crc = 0xFFFFFFFFu;

            for (var i = 4; i < 8; i++)
            {
                crc = CrcTable[(crc ^ header[i]) & 0xFF] ^ (crc >> 8);
            }

            foreach (var value in payload)
            {
                crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
            }

            var crcBytes = new byte[4];
            WriteInt32BE(crcBytes, 0, (int)(crc ^ 0xFFFFFFFFu));
            stream.Write(crcBytes, 0, 4);
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];

            for (uint n = 0; n < 256; n++)
            {
                var c = n;

                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }

        private static void WriteInt32BE(byte[] bytes, int offset, int value)
        {
            bytes[offset] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }
    }
}
