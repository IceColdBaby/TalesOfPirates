using System.Numerics;

namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// Full mesh payload of an .lgo/.lmo geometry object.
    /// <br/> lwMeshInfo (lwITypes2.h)
    /// </summary>
    public class Mesh
    {
        public uint Fvf;
        public uint PointType;
        public uint BoneInfluenceFactor;
        public int LegacyMeshVersion;
        public RenderStateAtom[] RenderStateSet;
        public VertexElement[] VertexElements;
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[][] TextureCoordinates;
        public uint[] VertexColors;
        public uint[] Indices;
        public SkinBlend[] SkinBlends;
        public uint[] BoneIndices;
        public MeshSubset[] Subsets;
    }
}
