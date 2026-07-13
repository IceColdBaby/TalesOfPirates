using System.IO;
using System.Text;

namespace Top.MindPower.World
{
    /// <summary>
    /// A .atr server terrain-attribute file.
    /// <br/> CTerrainAttrib (TerrainAttrib.cpp)
    /// </summary>
    public class AtrFile
    {
        private const int MaxDimension = 65536;

        public TerrainGrid Grid;

        public static AtrFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            var width = (int)r.ReadUInt32();
            var height = (int)r.ReadUInt32();

            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
            {
                throw new ParseException("atr", 0, r.BaseStream.Position,
                    $"implausible dimensions {width}x{height}");
            }

            var count = width * height;
            var tiles = new TerrainTile[count];

            for (var i = 0; i < count; i++)
            {
                tiles[i] = r.ReadTerrainTile();
            }

            return new AtrFile { Grid = new TerrainGrid(width, height, tiles) };
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write((uint)Grid.Width);
            w.Write((uint)Grid.Height);

            var tiles = Grid.Tiles;

            foreach (var terrainTile in tiles)
            {
                w.WriteTerrainTile(terrainTile);
            }

            w.Flush();
        }
    }
}
