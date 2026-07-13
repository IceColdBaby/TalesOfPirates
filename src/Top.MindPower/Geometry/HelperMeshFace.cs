using System.Numerics;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// One face of a helper mesh.
    /// <br/> lwHelperMeshFaceInfo (lwITypes2.h)
    /// </summary>
    public struct HelperMeshFace
    {
        public uint[] Vertex;
        public uint[] AdjFace;
        public Vector4 Plane;
        public Vector3 Center;
    }
}
