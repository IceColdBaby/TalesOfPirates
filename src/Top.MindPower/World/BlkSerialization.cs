using System.IO;

namespace Top.MindPower.World
{
    /// <summary>
    /// The bit-packed obstacle grid that follows the two int32 dimensions.
    /// <br/> CBlockData::Load (util2.h)
    /// </summary>
    internal static class BlkSerialization
    {
        internal static byte[] ReadPackedGrid(this BinaryReader r, int byteWidth, int height)
        {
            return r.ReadBytes(byteWidth * height);
        }

        internal static void WritePackedGrid(this BinaryWriter w, byte[] packedRows)
        {
            w.Write(packedRows);
        }
    }
}
