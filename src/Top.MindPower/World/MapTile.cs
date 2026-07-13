namespace Top.MindPower.World
{
    /// <summary>
    /// One terrain tile.
    /// <br/> SNewFileTile / SFileTile (MPMapDef.h)
    /// </summary>
    public class MapTile
    {
        public byte Texture0;
        public byte Alpha0;
        public byte Texture1;
        public byte Alpha1;
        public byte Texture2;
        public byte Alpha2;
        public byte Texture3;
        public byte Alpha3;

        public short Color565;
        public sbyte HeightStep;

        public short HeightCm;
        public uint Color888;

        public short Region;
        public byte Island;
        public byte[] Block;
    }
}
