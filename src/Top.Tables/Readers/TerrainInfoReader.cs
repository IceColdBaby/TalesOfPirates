using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class TerrainInfoReader
    {
        public static TerrainInfoRecord Read(TableRow row)
        {
            return new TerrainInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Type = row.NextEnum<TerrainType>(),
                LeavesFootprints = row.NextBool()
            };
        }
    }
}
