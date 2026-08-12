using System.Numerics;

namespace Top.Legacy.MindPower.Effects
{
    /// <summary>
    /// One sub-effect within an .eff file.
    /// <br/> I_Effect::LoadFromFile/Save (I_Effect.cpp)
    /// </summary>
    public class Effect
    {
        public string Name;
        public EffectType EffectType;
        public int SourceBlend;
        public int DestinationBlend;
        public float Length;

        public float[] FrameTime;
        public Vector3[] FrameSize;
        public Vector3[] FrameAngle;
        public Vector3[] FramePosition;
        public RgbaF[] FrameColor;

        public ushort TextureCoordinateVertexCount;
        public float TextureCoordinateFrameTime;
        public Vector2[][] TextureCoordinateLists;

        public float TextureFrameTime;
        public string TextureName;
        public Vector2[][] TextureLists;

        public string ModelName;
        public byte Billboard;
        public int VertexShaderIndex;

        public int SegmentCount;
        public float Height;
        public float TopRadius;
        public float BottomRadius;

        public float TextureFrameTimeA;
        public string[] TextureFrameNames;
        public float TextureFrameTimeB;

        public int UseParameter;
        public EffectCylinderParameters[] CylinderParameters;

        public byte RotationLoop;
        public Vector4 RotationLoopVector;

        public byte Alpha;

        public byte RotationBoard;
    }
}
