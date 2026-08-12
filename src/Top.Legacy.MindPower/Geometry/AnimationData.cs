using Top.Legacy.MindPower.Animation;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// Every animation channel of a geometry object (16 subsets x 4 stages).
    /// <br/> lwAnimDataInfo (lwExpObj.h)
    /// </summary>
    public sealed class AnimationData
    {
        public BoneAnimation Bone;
        public MatrixAnimation Matrix;
        public MaterialOpacityAnimation[] MaterialOpacity;
        public TextureUvAnimation[,] TextureUv;
        public TextureImageAnimation[,] TextureImage;
    }
}
