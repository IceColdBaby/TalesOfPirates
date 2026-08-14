namespace Top.Contracts.Assets.Maps
{
    /// <summary>
    /// Represents a single tile in a map.
    /// </summary>
    public struct MapTile
    {
        public float Height;
        public byte ColorR;
        public byte ColorG;
        public byte ColorB;
        public MapTileLayer Layer0;
        public MapTileLayer Layer1;
        public MapTileLayer Layer2;
        public MapTileLayer Layer3;
        public ushort Region;
        public byte Island;
        public byte Corner00;
        public byte Corner10;
        public byte Corner01;
        public byte Corner11;
    }
}
