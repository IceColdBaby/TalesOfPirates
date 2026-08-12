namespace Top.Legacy.MindPower.Geometry
{
    /// <summary>
    /// A mesh subset (one material's index range).
    /// <br/> lwSubsetInfo (lwITypes2.h)
    /// </summary>
    public struct MeshSubset
    {
        public uint PrimitiveCount;
        public uint StartIndex;
        public uint VertexCount;
        public uint MinIndex;
    }
}
