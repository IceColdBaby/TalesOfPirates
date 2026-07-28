using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class SailLevelUpReader
    {
        public static SailLevelUpRecord Read(TableRow row)
        {
            return new SailLevelUpRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Exp = row.NextLong(),
            };
        }
    }
}
