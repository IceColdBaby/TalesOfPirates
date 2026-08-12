namespace Top.Legacy.Tables.Records
{
    public class ShadeInfoRecord : TableRecord
    {
        public string DisplayName = string.Empty;
        public float Size;
        public bool Animated;
        public int Rows;
        public int Cols;
        public bool UseAlphaTest;
        public int AlphaType;
        public int[] Color = System.Array.Empty<int>();
        public int Type;
    }
}
