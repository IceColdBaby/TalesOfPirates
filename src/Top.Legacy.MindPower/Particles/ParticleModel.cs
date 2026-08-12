using Top.Legacy.MindPower.Geometry;

namespace Top.Legacy.MindPower.Particles
{
    /// <summary>
    /// Embedded 3D model attachment.
    /// <br/> CChaModel::LoadFromFile/SaveToFile (MPParticleCtrl.cpp)
    /// </summary>
    public class ParticleModel
    {
        public int Id;
        public float Velocity;
        public AnimationPlayType PlayType;
        public int CurrentPose;
        public int SourceBlend;
        public int DestinationBlend;
        public RgbaF CurrentColor;
    }
}
