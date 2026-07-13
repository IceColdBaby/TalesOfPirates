namespace Top.MindPower.World
{
    /// <summary>
    /// A row-major terrain-attribute grid.
    /// <br/> CTerrainAttrib (TerrainAttrib.cpp)
    /// </summary>
    public readonly struct TerrainGrid
    {
        public TerrainGrid(int width, int height, TerrainTile[] tiles)
        {
            Width = width;
            Height = height;
            Tiles = tiles;
        }

        public int Width { get; }
        public int Height { get; }
        public TerrainTile[] Tiles { get; }

        public TerrainTile this[int x, int y]
        {
            get
            {
                return Tiles[(y * Width) + x];
            }
            set
            {
                Tiles[(y * Width) + x] = value;
            }
        }
    }
}
