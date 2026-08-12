namespace Top.Legacy.MindPower.World
{
    /// <summary>
    /// A bit-packed obstacle grid.
    /// <br/> CBlockData (util2.h)
    /// </summary>
    public readonly struct BlockGrid
    {
        public BlockGrid(int width, int height, byte[] packedRows)
        {
            Width = width;
            Height = height;
            PackedRows = packedRows;
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] PackedRows { get; }

        public int ByteWidth => Width / 8;

        public bool IsBlocked(int x, int y)
        {
            var bit = 7 - (x % 8);
            var data = PackedRows[(ByteWidth * y) + (x / 8)];

            return (data & (1 << bit)) != 0;
        }

        public void SetBlocked(int x, int y, bool blocked)
        {
            var bit = 7 - (x % 8);
            var index = (ByteWidth * y) + (x / 8);
            var mask = (byte)(1 << bit);

            if (blocked)
            {
                PackedRows[index] |= mask;
            }
            else
            {
                PackedRows[index] = (byte)(PackedRows[index] & ~mask);
            }
        }
    }
}
