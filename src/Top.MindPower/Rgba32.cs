namespace Top.MindPower
{
    /// <summary>
    /// 8-bit RGBA.
    /// </summary>
    public readonly struct Rgba32
    {
        public readonly byte R, G, B, A;

        public Rgba32(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Rgba32 FromArgb(uint argb)
        {
            return new Rgba32((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24));
        }

        public uint ToArgb()
        {
            return ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;
        }
    }
}
