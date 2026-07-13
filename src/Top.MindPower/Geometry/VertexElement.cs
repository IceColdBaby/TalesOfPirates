namespace Top.MindPower.Geometry
{
    /// <summary>
    /// Vertex declaration element parsed by lwMeshInfo.
    /// <br/> D3DVERTEXELEMENT9 (d3d9types.h)
    /// </summary>
    public struct VertexElement
    {
        public ushort Stream;
        public ushort Offset;
        public byte Type;
        public byte Method;
        public byte Usage;
        public byte UsageIndex;
    }
}
