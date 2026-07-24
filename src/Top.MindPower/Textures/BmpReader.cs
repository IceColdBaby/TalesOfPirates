using System;
using System.IO;

namespace Top.MindPower.Textures
{
    /// <summary>
    /// Decodes uncompressed BMP: 8-bit palettized, 16-bit X1R5G5B5, 24-bit and
    /// 32-bit, including 32-bit BI_BITFIELDS with the standard BGRX masks.
    /// BMP carries no alpha in the original pipeline (D3DX loads 32-bit BMP as
    /// X8R8G8B8), so every pixel decodes opaque; transparency comes from color
    /// keying.
    /// </summary>
    public static class BmpReader
    {
        public static RgbaImage Read(byte[] bytes)
        {
            bytes = TextureEncode.Decode(bytes);

            if (bytes.Length < 54 || bytes[0] != (byte)'B' || bytes[1] != (byte)'M')
            {
                throw new InvalidDataException("not a BMP file");
            }

            var pixelOffset = BitConverter.ToInt32(bytes, 10);
            var headerSize = BitConverter.ToInt32(bytes, 14);
            var width = BitConverter.ToInt32(bytes, 18);
            var rawHeight = BitConverter.ToInt32(bytes, 22);
            int bpp = BitConverter.ToUInt16(bytes, 28);
            var compression = BitConverter.ToInt32(bytes, 30);

            if (bpp != 8 && bpp != 16 && bpp != 24 && bpp != 32)
            {
                throw new InvalidDataException($"unsupported BMP bit count {bpp}");
            }

            if (compression == 3)
            {
                if (bpp != 32
                    || BitConverter.ToUInt32(bytes, 54) != 0x00FF0000
                    || BitConverter.ToUInt32(bytes, 58) != 0x0000FF00
                    || BitConverter.ToUInt32(bytes, 62) != 0x000000FF)
                {
                    throw new InvalidDataException("unsupported BMP bit fields");
                }
            }
            else if (compression != 0)
            {
                throw new InvalidDataException($"unsupported BMP compression {compression}");
            }

            var topDown = rawHeight < 0;
            var height = Math.Abs(rawHeight);
            var stride = ((width * bpp) + 31) / 32 * 4;
            var paletteOffset = 14 + headerSize + (compression == 3 && headerSize == 40 ? 12 : 0);
            var pixels = new Rgba32[width * height];

            for (var y = 0; y < height; y++)
            {
                var row = pixelOffset + ((topDown ? y : height - 1 - y) * stride);

                for (var x = 0; x < width; x++)
                {
                    Rgba32 pixel;

                    switch (bpp)
                    {
                        case 8:
                            {
                                var entry = paletteOffset + (bytes[row + x] * 4);
                                pixel = new Rgba32(bytes[entry + 2], bytes[entry + 1], bytes[entry], 255);
                                break;
                            }

                        case 16:
                            {
                                var value = BitConverter.ToUInt16(bytes, row + (x * 2));
                                var r = (value >> 10) & 31;
                                var g = (value >> 5) & 31;
                                var b = value & 31;
                                pixel = new Rgba32(
                                    (byte)((r << 3) | (r >> 2)),
                                    (byte)((g << 3) | (g >> 2)),
                                    (byte)((b << 3) | (b >> 2)), 255);
                                break;
                            }

                        case 24:
                            {
                                var o = row + (x * 3);
                                pixel = new Rgba32(bytes[o + 2], bytes[o + 1], bytes[o], 255);
                                break;
                            }

                        default:
                            {
                                var o = row + (x * 4);
                                pixel = new Rgba32(bytes[o + 2], bytes[o + 1], bytes[o], 255);
                                break;
                            }
                    }

                    pixels[(y * width) + x] = pixel;
                }
            }

            return new RgbaImage(width, height, pixels);
        }
    }
}
