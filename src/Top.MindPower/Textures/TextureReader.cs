using System;

namespace Top.MindPower.Textures
{
    /// <summary>
    /// Decodes a texture file by content: DDS and BMP are detected by magic,
    /// anything else is attempted as TGA (which has none). Extensions are not
    /// trusted; scene files named .bmp are DDS while item files are real BMP.
    /// </summary>
    public static class TextureReader
    {
        public static RgbaImage Read(byte[] bytes)
        {
            bytes = TextureEncode.Decode(bytes);

            if (bytes.Length >= 4 && BitConverter.ToUInt32(bytes, 0) == 0x20534444)
            {
                return DdsReader.Read(bytes);
            }

            if (bytes.Length >= 2 && bytes[0] == (byte)'B' && bytes[1] == (byte)'M')
            {
                return BmpReader.Read(bytes);
            }

            return TgaReader.Read(bytes);
        }
    }
}
