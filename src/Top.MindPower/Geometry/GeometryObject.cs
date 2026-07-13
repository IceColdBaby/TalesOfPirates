using System.Numerics;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// One geometry object.
    /// <br/> lwGeomObjInfo (lwExpObj.h)
    /// </summary>
    public class GeometryObject
    {
        public uint Version;
        public uint Id;
        public uint ParentId;
        public uint Type;
        public Matrix4x4 LocalMatrix;
        public uint MaterialVersion;
        public MaterialTexture[] Materials;
        public Mesh Mesh;
        public Helper Helper;
        public AnimationData Animation;
    }
}
