namespace Top.MindPower.Geometry
{
    /// <summary>
    /// Per-stage texture image-swap track (each frame is a full lwTexInfo).
    /// <br/> lwAnimDataTexImg (lwExpObj.h)
    /// </summary>
    public class TextureImageAnimation
    {
        public int Subset;
        public int Stage;
        public TextureStage[] DataSequence;
    }
}
