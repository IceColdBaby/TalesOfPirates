using System.IO;
using System.Text;

namespace Top.MindPower.World
{
    /// <summary>
    /// A .blk server obstacle bitmap.
    /// <br/> CBlockData::Load (util2.h)
    /// </summary>
    public class BlkFile
    {
        private const int MaxDimension = 65536;

        public BlockGrid Grid;

        public static BlkFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var width = r.ReadInt32();
            var height = r.ReadInt32();

            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension
                || width % 8 != 0)
            {
                throw new ParseException("blk", 0, r.BaseStream.Position,
                    $"implausible dimensions {width}x{height}");
            }

            return new BlkFile
            {
                Grid = new BlockGrid(width, height, r.ReadPackedGrid(width / 8, height)),
            };
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(Grid.Width);
            w.Write(Grid.Height);
            w.WritePackedGrid(Grid.PackedRows);
            w.Flush();
        }
    }
}
