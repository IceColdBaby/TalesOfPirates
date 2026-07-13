using System.Numerics;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// Per-stage texture-UV transform track.
    /// <br/> lwAnimDataTexUV (lwExpObj.h)
    /// </summary>
    public sealed class TextureUvAnimation
    {
        public int Subset;
        public int Stage;
        public Matrix4x4[] Frames;
    }
}
