using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class MonsterInfoReader
    {
        public static MonsterInfoRecord Read(TableRow row)
        {
            var record = new MonsterInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString()
            };

            int[] start = row.NextIntList();

            if (start.Length >= 2)
            {
                record.StartX = start[0] / 100;
                record.StartY = start[1] / 100;
            }

            int[] end = row.NextIntList();

            if (end.Length >= 2)
            {
                record.EndX = end[0] / 100;
                record.EndY = end[1] / 100;
            }

            record.MonsterIds = row.NextIntList();
            record.MapName = row.NextString();

            return record;
        }
    }
}
