using System.Numerics;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// A DUMMY scene node: an id, a local matrix, and an optional per-frame matrix animation.
    /// <br/> lwHelperDummyObjInfo (lwExpObj.cpp)
    /// </summary>
    public class SceneDummy
    {
        public uint Id;
        public Matrix4x4 Local;
        public MatrixAnimation Animation;
    }
}
