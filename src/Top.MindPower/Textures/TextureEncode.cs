using System;

namespace Top.MindPower.Textures
{
    /// <summary>
    /// Legacy texture obfuscation: encoded files end with "mp.x"; decode drops
    /// the marker and swaps the first 44 bytes with the last 44.
    /// - lwTexEncode (lwFileEncode.cpp)
    /// </summary>
    public static class TextureEncode
    {
        private const int SwapLength = 44;

        public static byte[] Decode(byte[] bytes)
        {
            var length = bytes.Length;

            if (length < 4 || bytes[length - 4] != (byte)'m'
                           || bytes[length - 3] != (byte)'p'
                           || bytes[length - 2] != (byte)'.'
                           || bytes[length - 1] != (byte)'x')
            {
                return bytes;
            }

            var result = new byte[length - 4];
            Array.Copy(bytes, result, length - 4);

            if (result.Length > SwapLength)
            {
                var tmp = new byte[SwapLength];
                Array.Copy(result, tmp, SwapLength);
                Array.Copy(result, result.Length - SwapLength, result, 0, SwapLength);
                tmp.CopyTo(result, result.Length - SwapLength);
            }

            return result;
        }
    }
}
