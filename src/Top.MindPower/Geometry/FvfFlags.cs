namespace Top.MindPower.Geometry
{
    /// <summary>
    /// D3DFVF_* flexible-vertex-format flag bits (d3d9types.h).
    /// </summary>
    public static class FvfFlags
    {
        public const uint Normal = 16;
        public const uint Diffuse = 64;
        public const uint TexMask = 3840;
        public const uint Tex1 = 256;
        public const uint Tex2 = 512;
        public const uint Tex3 = 768;
        public const uint Tex4 = 1024;
        public const uint LastBetaUbyte4 = 4096;
    }
}
