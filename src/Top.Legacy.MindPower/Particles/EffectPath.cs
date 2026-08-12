using System.Numerics;

namespace Top.Legacy.MindPower.Particles
{
    /// <summary>
    /// Spline path particles follow.
    /// <br/> CEffPath::LoadPath (MPModelEff.cpp)
    /// </summary>
    public class EffectPath
    {
        public float Velocity;
        public Vector3[] PathPoints;
        public Vector3[] Directions;
        public EffectPathDistanceSlot[] Distances;
    }
}
