using System;
using System.IO;

namespace Top.MindPower.Textures
{
    /// <summary>
    /// Decodes DDS (DXT1/DXT3/DXT5 and uncompressed BGR/BGRA) top-level mip.
    /// </summary>
    public static class DdsReader
    {
        public static RgbaImage Read(byte[] bytes)
        {
            bytes = TextureEncode.Decode(bytes);

            if (bytes.Length < 128 || BitConverter.ToUInt32(bytes, 0) != 0x20534444)
            {
                throw new InvalidDataException("not a DDS file");
            }

            var height = BitConverter.ToInt32(bytes, 12);
            var width = BitConverter.ToInt32(bytes, 16);
            var pfFlags = BitConverter.ToUInt32(bytes, 80);
            var fourCc = BitConverter.ToUInt32(bytes, 84);
            var pixels = new Rgba32[width * height];

            if ((pfFlags & 0x4) != 0)
            {
                switch (fourCc)
                {
                    case 0x31545844:
                        DecodeBlocks(bytes, 128, width, height, pixels, 8, DecodeBc1Block);
                        break;
                    case 0x33545844:
                        DecodeBlocks(bytes, 128, width, height, pixels, 16, DecodeBc2Block);
                        break;
                    case 0x35545844:
                        DecodeBlocks(bytes, 128, width, height, pixels, 16, DecodeBc3Block);
                        break;
                    default:
                        throw new InvalidDataException($"unsupported DDS fourCC 0x{fourCc:X8}");
                }
            }
            else if ((pfFlags & 0x40) != 0)
            {
                var bitCount = BitConverter.ToInt32(bytes, 88);
                var bytesPerPixel = bitCount / 8;

                if (bitCount != 24 && bitCount != 32)
                {
                    throw new InvalidDataException($"unsupported RGB bit count {bitCount}");
                }

                for (var i = 0; i < pixels.Length; i++)
                {
                    var o = 128 + (i * bytesPerPixel);

                    pixels[i] = new Rgba32(bytes[o + 2], bytes[o + 1], bytes[o],
                        bitCount == 32 ? bytes[o + 3] : (byte)255);
                }
            }
            else
            {
                throw new InvalidDataException("unsupported DDS pixel format");
            }

            return new RgbaImage(width, height, pixels);
        }

        private delegate void BlockDecoder(byte[] bytes, int offset, Rgba32[] block16);

        private static void DecodeBlocks(byte[] bytes, int offset, int width, int height,
            Rgba32[] pixels, int blockSize, BlockDecoder decode)
        {
            var block = new Rgba32[16];

            for (var by = 0; by < (height + 3) / 4; by++)
            {
                for (var bx = 0; bx < (width + 3) / 4; bx++)
                {
                    decode(bytes, offset, block);

                    offset += blockSize;

                    for (var py = 0; py < 4; py++)
                    {
                        for (var px = 0; px < 4; px++)
                        {
                            var x = (bx * 4) + px;
                            var y = (by * 4) + py;

                            if (x < width && y < height)
                            {
                                pixels[(y * width) + x] = block[(py * 4) + px];
                            }
                        }
                    }
                }
            }
        }

        private static void DecodeBc1Block(byte[] bytes, int offset, Rgba32[] block)
        {
            DecodeColorBlock(bytes, offset, block, allowPunchThrough: true);
        }

        private static void DecodeBc2Block(byte[] bytes, int offset, Rgba32[] block)
        {
            DecodeColorBlock(bytes, offset + 8, block, allowPunchThrough: false);

            for (var i = 0; i < 16; i++)
            {
                var nibble = (bytes[offset + (i / 2)] >> ((i % 2) * 4)) & 0xF;
                block[i] = new Rgba32(block[i].R, block[i].G, block[i].B, (byte)(nibble * 17));
            }
        }

        private static void DecodeBc3Block(byte[] bytes, int offset, Rgba32[] block)
        {
            DecodeColorBlock(bytes, offset + 8, block, allowPunchThrough: false);

            var alpha0 = bytes[offset];
            var alpha1 = bytes[offset + 1];
            var alphas = new byte[8];

            alphas[0] = alpha0;
            alphas[1] = alpha1;

            if (alpha0 > alpha1)
            {
                for (var i = 1; i < 7; i++)
                {
                    alphas[i + 1] = (byte)((((7 - i) * alpha0) + (i * alpha1)) / 7);
                }
            }
            else
            {
                for (var i = 1; i < 5; i++)
                {
                    alphas[i + 1] = (byte)((((5 - i) * alpha0) + (i * alpha1)) / 5);
                }

                alphas[6] = 0;
                alphas[7] = 255;
            }

            ulong bits = 0;

            for (var i = 0; i < 6; i++)
            {
                bits |= (ulong)bytes[offset + 2 + i] << (8 * i);
            }

            for (var i = 0; i < 16; i++)
            {
                var a = alphas[(bits >> (3 * i)) & 0x7];
                block[i] = new Rgba32(block[i].R, block[i].G, block[i].B, a);
            }
        }

        private static void DecodeColorBlock(
            byte[] bytes, int offset, Rgba32[] block, bool allowPunchThrough)
        {
            var c0 = BitConverter.ToUInt16(bytes, offset);
            var c1 = BitConverter.ToUInt16(bytes, offset + 2);

            var colors = new Rgba32[4];
            colors[0] = From565(c0);
            colors[1] = From565(c1);

            if (c0 > c1 || !allowPunchThrough)
            {
                colors[2] = Lerp(colors[0], colors[1], 1, 3);
                colors[3] = Lerp(colors[0], colors[1], 2, 3);
            }
            else
            {
                colors[2] = Lerp(colors[0], colors[1], 1, 2);
                colors[3] = new Rgba32(0, 0, 0, 0);
            }

            var indices = BitConverter.ToUInt32(bytes, offset + 4);

            for (var i = 0; i < 16; i++)
            {
                block[i] = colors[(indices >> (2 * i)) & 0x3];
            }
        }

        private static Rgba32 From565(ushort value)
        {
            var r = (value >> 11) & 0x1F;
            var g = (value >> 5) & 0x3F;
            var b = value & 0x1F;

            return new Rgba32(
                (byte)((r * 255 + 15) / 31),
                (byte)((g * 255 + 31) / 63),
                (byte)((b * 255 + 15) / 31),
                255);
        }

        private static Rgba32 Lerp(Rgba32 a, Rgba32 b, int num, int den)
        {
            return new Rgba32(
                (byte)((((den - num) * a.R) + (num * b.R)) / den),
                (byte)((((den - num) * a.G) + (num * b.G)) / den),
                (byte)((((den - num) * a.B) + (num * b.B)) / den),
                255);
        }
    }
}
