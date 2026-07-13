using System.Numerics;

namespace Top.MindPower
{
    /// <summary>
    /// lwBoundingBoxInfo (lwITypes2.h)
    /// </summary>
    public struct BoundingBox
    {
        public uint Id;
        public Vector3 BoxCenter;
        public Vector3 BoxExtents;
        public Matrix4x4 Matrix;
    }
}
