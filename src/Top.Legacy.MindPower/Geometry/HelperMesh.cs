using System.Numerics;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// A helper collision/marker mesh.
    /// <br/> lwHelperMeshInfo (lwITypes2.h)
    /// </summary>
    public struct HelperMesh
    {
        public uint Id;
        public uint Type;
        public uint SubType;
        public string Name;
        public uint State;
        public Matrix4x4 Matrix;
        public Vector3 BoxCenter;
        public Vector3 BoxExtents;
        public Vector3[] Vertices;
        public HelperMeshFace[] Faces;
    }
}
