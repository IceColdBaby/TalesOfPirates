using System.Numerics;

namespace Top.MindPower.Animation
{
    /// <summary>
    /// One bone descriptor.
    /// <br/> lwBoneBaseInfo (lwExpObj.h)
    /// </summary>
    public class Bone
    {
        public string Name;
        public int Id;
        public int ParentId;
        public Matrix4x4 InvBindMatrix;
    }
}
