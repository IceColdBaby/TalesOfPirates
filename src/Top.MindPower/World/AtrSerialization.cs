using System.IO;

namespace Top.MindPower.World
{
    /// <summary>
    /// One STILE_ATTRIB (TerrainAttrib.h:63)
    /// </summary>
    internal static class AtrSerialization
    {
        internal static TerrainTile ReadTerrainTile(this BinaryReader r)
        {
            var attrib = r.ReadUInt16();
            var island = r.ReadByte();

            return new TerrainTile
            {
                Attrib = (TerrainAttribute)attrib,
                Island = island,
            };
        }

        internal static void WriteTerrainTile(this BinaryWriter w, TerrainTile tile)
        {
            w.Write((ushort)tile.Attrib);
            w.Write(tile.Island);
        }
    }
}
