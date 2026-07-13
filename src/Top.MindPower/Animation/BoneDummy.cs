using System.Numerics;

namespace Top.MindPower.Animation
{
    /// <summary>
    /// A skeleton attachment point.
    /// <br/> lwBoneDummyInfo (lwExpObj.h)
    /// </summary>
    public struct BoneDummy
    {
        public uint Id;
        public uint ParentBoneId;
        public Matrix4x4 Matrix;
    }
}
