using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class NpcListReader
    {
        public static NpcListRecord Read(TableRow row)
        {
            var record = new NpcListRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Area = row.NextString()
            };

            int[] position = row.NextIntList();
            record.X = position.Length > 0 ? position[0] : 0;
            record.Y = position.Length > 1 ? position[1] : 0;
            record.MapName = row.NextString();

            return record;
        }
    }
}
