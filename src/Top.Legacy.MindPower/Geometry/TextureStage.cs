namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// One texture stage; reused for TextureImageAnimation frames.
    /// <br/> lwTexInfo (lwITypes2.h)
    /// </summary>
    public class TextureStage
    {
        public uint Stage;
        public uint Level;
        public uint Usage;
        public uint Format;
        public uint Pool;
        public uint ByteAlignmentFlag;
        public uint Type;
        public uint Width;
        public uint Height;
        public ColorKeyType ColorKeyType;
        public Rgba32 ColorKey;
        public string FileName;
        public RenderStateAtom[] TssSet;
    }
}
