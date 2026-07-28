namespace Top.Tables.Records
{
    public enum TerrainType
    {
        Normal = 0,
        Underwater = 1,
    }

    public class TerrainInfoRecord : TableRecord
    {
        public TerrainType Type;
        public bool LeavesFootprints;
    }
}
