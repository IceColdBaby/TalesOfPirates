using System;
using System.IO;

namespace Top.MindPower.Textures
{
    /// <summary>
    /// Decodes TGA types 1/2/9/10: color-mapped and truecolor, raw and RLE,
    /// at 8, 24 or 32 bits per pixel. 32-bit pixels keep their alpha channel.
    /// TGA carries no magic, so anything long enough is accepted and other
    /// formats decode into noise rather than failing. Prefer
    /// <see cref="TextureReader"/>, which picks the reader by content.
    /// </summary>
    public static class TgaReader
    {
        public static RgbaImage Read(byte[] bytes)
        {
            bytes = TextureEncode.Decode(bytes);

            if (bytes.Length < 18)
            {
                throw new InvalidDataException("not a TGA file");
            }

            int idLength = bytes[0];
            int colorMapType = bytes[1];
            int imageType = bytes[2];
            int mapStart = BitConverter.ToUInt16(bytes, 3);
            int mapLength = BitConverter.ToUInt16(bytes, 5);
            int mapEntryBits = bytes[7];
            int width = BitConverter.ToUInt16(bytes, 12);
            int height = BitConverter.ToUInt16(bytes, 14);
            int bpp = bytes[16];
            int descriptor = bytes[17];

            var colorMapped = imageType == 1 || imageType == 9;
            var truecolor = imageType == 2 || imageType == 10;

            if ((!colorMapped && !truecolor) || width == 0 || height == 0
                || (colorMapped && (colorMapType != 1 || bpp != 8
                                                      || (mapEntryBits != 24 && mapEntryBits != 32)))
                || (truecolor && bpp != 24 && bpp != 32))
            {
                throw new InvalidDataException("not a supported TGA file");
            }

            var mapEntrySize = mapEntryBits / 8;
            var mapOffset = 18 + idLength;
            var mapEnd = mapOffset + (colorMapType == 1 ? mapLength * mapEntrySize : 0);
            var pixelSize = bpp / 8;
            var data = imageType >= 9
                ? DecodeRle(bytes, mapEnd, width * height, pixelSize)
                : ReadRaw(bytes, mapEnd, width * height * pixelSize);

            var topDown = (descriptor & 0x20) != 0;
            var pixels = new Rgba32[width * height];

            for (var y = 0; y < height; y++)
            {
                var sourceY = topDown ? y : height - 1 - y;

                for (var x = 0; x < width; x++)
                {
                    var o = ((sourceY * width) + x) * pixelSize;
                    Rgba32 pixel;

                    if (colorMapped)
                    {
                        var entry = mapOffset + ((data[o] - mapStart) * mapEntrySize);

                        if (entry < mapOffset || entry + mapEntrySize > mapEnd)
                        {
                            throw new InvalidDataException("TGA color-map index out of range");
                        }

                        pixel = new Rgba32(bytes[entry + 2], bytes[entry + 1], bytes[entry],
                            mapEntrySize == 4 ? bytes[entry + 3] : (byte)255);
                    }
                    else
                    {
                        pixel = new Rgba32(data[o + 2], data[o + 1], data[o],
                            pixelSize == 4 ? data[o + 3] : (byte)255);
                    }

                    pixels[(y * width) + x] = pixel;
                }
            }

            return new RgbaImage(width, height, pixels);
        }

        private static byte[] ReadRaw(byte[] bytes, int offset, int length)
        {
            if (offset + length > bytes.Length)
            {
                throw new InvalidDataException("truncated TGA pixel data");
            }

            var data = new byte[length];
            Array.Copy(bytes, offset, data, 0, length);

            return data;
        }

        private static byte[] DecodeRle(byte[] bytes, int offset, int pixelCount, int pixelSize)
        {
            var data = new byte[pixelCount * pixelSize];
            var written = 0;

            while (written < data.Length)
            {
                if (offset >= bytes.Length)
                {
                    throw new InvalidDataException("truncated TGA RLE data");
                }

                var header = bytes[offset++];
                var count = (header & 0x7F) + 1;

                if (written + (count * pixelSize) > data.Length)
                {
                    throw new InvalidDataException("TGA RLE data overruns the image");
                }

                if ((header & 0x80) != 0)
                {
                    if (offset + pixelSize > bytes.Length)
                    {
                        throw new InvalidDataException("truncated TGA RLE data");
                    }

                    for (var i = 0; i < count; i++)
                    {
                        Array.Copy(bytes, offset, data, written, pixelSize);
                        written += pixelSize;
                    }

                    offset += pixelSize;
                }
                else
                {
                    var length = count * pixelSize;

                    if (offset + length > bytes.Length)
                    {
                        throw new InvalidDataException("truncated TGA RLE data");
                    }

                    Array.Copy(bytes, offset, data, written, length);
                    written += length;
                    offset += length;
                }
            }

            return data;
        }
    }
}
