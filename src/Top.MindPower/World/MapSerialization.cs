using System.IO;

namespace Top.MindPower.World
{
    /// <summary>
    /// One terrain tile's on-disk block.
    /// <br/> SNewFileTile/SFileTile (MPMapDef.h)
    /// </summary>
    internal static class MapSerialization
    {
        internal const int TilesPerSection = 64;
        internal const int BlockBytes = 4;

        internal static MapTile ReadNewTile(this BinaryReader r)
        {
            var tileInfo = r.ReadUInt32();
            var baseTexture = r.ReadByte();

            var tile = new MapTile
            {
                Texture0 = baseTexture,
                Alpha0 = 15,
                Texture1 = (byte)((tileInfo >> 26) & 0x3F),
                Alpha1 = (byte)((tileInfo >> 22) & 0x0F),
                Texture2 = (byte)((tileInfo >> 16) & 0x3F),
                Alpha2 = (byte)((tileInfo >> 12) & 0x0F),
                Texture3 = (byte)((tileInfo >> 6) & 0x3F),
                Alpha3 = (byte)((tileInfo >> 2) & 0x0F),
                Color565 = r.ReadInt16(),
                HeightStep = r.ReadSByte(),
                Region = r.ReadInt16(),
                Island = r.ReadByte(),
                Block = r.ReadBytes(BlockBytes),
            };

            return tile;
        }

        internal static void WriteNewTile(this BinaryWriter w, MapTile tile)
        {
            var tileInfo = ((uint)tile.Texture1 << 26) | ((uint)tile.Alpha1 << 22)
                                                       | ((uint)tile.Texture2 << 16) | ((uint)tile.Alpha2 << 12)
                                                       | ((uint)tile.Texture3 << 6) | ((uint)tile.Alpha3 << 2);
            w.Write(tileInfo);
            w.Write(tile.Texture0);
            w.Write(tile.Color565);
            w.Write(tile.HeightStep);
            w.Write(tile.Region);
            w.Write(tile.Island);
            w.Write(tile.Block);
        }

        internal static MapTile ReadOldTile(this BinaryReader r)
        {
            var t = r.ReadBytes(8);

            var tile = new MapTile
            {
                Texture0 = t[0],
                Alpha0 = t[1],
                Texture1 = t[2],
                Alpha1 = t[3],
                Texture2 = t[4],
                Alpha2 = t[5],
                Texture3 = t[6],
                Alpha3 = t[7],
                HeightCm = r.ReadInt16(),
                Color888 = r.ReadUInt32(),
                Region = r.ReadInt16(),
                Island = r.ReadByte(),
                Block = r.ReadBytes(BlockBytes),
            };

            return tile;
        }

        internal static void WriteOldTile(this BinaryWriter w, MapTile tile)
        {
            w.Write(tile.Texture0);
            w.Write(tile.Alpha0);
            w.Write(tile.Texture1);
            w.Write(tile.Alpha1);
            w.Write(tile.Texture2);
            w.Write(tile.Alpha2);
            w.Write(tile.Texture3);
            w.Write(tile.Alpha3);
            w.Write(tile.HeightCm);
            w.Write(tile.Color888);
            w.Write(tile.Region);
            w.Write(tile.Island);
            w.Write(tile.Block);
        }
    }
}
