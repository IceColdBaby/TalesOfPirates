using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class LifeLevelUpReader
    {
        public static LifeLevelUpRecord Read(TableRow row)
        {
            var record = new LifeLevelUpRecord();

            record.Id = row.NextInt();
            record.Level = row.NextInt();
            record.Name = record.Level.ToString();
            record.Exp = row.NextInt();

            return record;
        }
    }
}
