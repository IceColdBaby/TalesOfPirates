using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ForgeItemReader
    {
        public static ForgeItemRecord Read(TableRow row)
        {
            var record = new ForgeItemRecord();

            record.Id = record.Level = row.NextInt();
            record.Name = record.Id.ToString();
            record.FailedLevel = row.NextInt();
            record.SuccessRate = row.NextInt();

            if (record.SuccessRate > 100)
            {
                record.SuccessRate = 100;
            }

            record.Items = new ForgeItemEntry[6];

            for (int i = 0; i < 6; i++)
            {
                int[] pair = row.NextIntList();

                record.Items[i] = new ForgeItemEntry
                {
                    Id = pair.Length > 0 ? pair[0] : 0,
                    Count = pair.Length > 1 ? pair[1] : 0
                };
            }

            record.Money = row.NextInt();

            return record;
        }
    }
}
