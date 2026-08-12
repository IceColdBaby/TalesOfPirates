using System.Numerics;

namespace Top.Legacy.MindPower.Particles
{
    /// <summary>
    /// One particle emitter in a .par file.
    /// <br/> CMPPartSys::LoadFromFile/SaveToFile (MPParticleSys.cpp)
    /// </summary>
    public class ParticleEmitter
    {
        public ParticleSystemType Type;
        public string PartName;
        public int ParticleCount;
        public string TextureName;
        public string ModelName;

        /// Length 3: spawn-box [width, height, depth].
        public float[] Range;

        public float[] FrameSize;
        public Vector3[] FrameAngle;
        public RgbaF[] FrameColor;
        public byte Billboard;
        public int SourceBlend;
        public int DestinationBlend;
        public int MinFilter;
        public int MagFilter;
        public float Life;
        public float Velocity;
        public Vector3 Direction;
        public Vector3 Acceleration;
        public float Step;

        public byte ModelRangeFlag;
        public string ModelRangeName;

        public Vector3 Offset;

        public float DelayTime;
        public float PlayTime;

        public byte UsePath;
        public EffectPath Path;

        public byte Shade;

        public string HitEffectName;

        public Vector3[] PointRanges;

        public int Roadom;

        public byte ModelDirection;

        public byte Mediay;
    }
}
