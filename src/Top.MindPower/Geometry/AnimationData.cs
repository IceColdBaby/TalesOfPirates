using Top.MindPower.Animation;

namespace Top.MindPower.Geometry
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
