using System.Numerics;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// A dummy locator node.
    /// <br/> lwHelperDummyInfo (lwITypes2.h)
    /// </summary>
    public struct HelperDummy
    {
        public uint Id;
        public Matrix4x4 Matrix;
        public Matrix4x4 LocalMatrix;
        public uint ParentType;
        public uint ParentId;
    }
}
