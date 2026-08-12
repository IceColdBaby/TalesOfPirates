using System.Numerics;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// An oriented helper box.
    /// <br/> lwHelperBoxInfo (lwITypes2.h)
    /// </summary>
    public struct HelperBox
    {
        public uint Id;
        public uint Type;
        public uint State;
        public Vector3 BoxCenter;
        public Vector3 BoxExtents;
        public Matrix4x4 Matrix;
        public string Name;
    }
}
