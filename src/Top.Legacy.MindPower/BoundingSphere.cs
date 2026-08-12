using System.Numerics;

namespace Top.Legacy.MindPower
{
    /// <summary>
    /// lwBoundingSphereInfo (lwITypes2.h)
    /// </summary>
    public struct BoundingSphere
    {
        public uint Id;
        public Vector3 Center;
        public float Radius;
        public Matrix4x4 Matrix;
    }
}
