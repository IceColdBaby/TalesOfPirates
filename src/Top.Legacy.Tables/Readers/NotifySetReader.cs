using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class NotifySetReader
    {
        public static NotifyRecord Read(TableRow row)
        {
            return new NotifyRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Type = row.NextInt(),
                Message = row.NextString()
            };
        }
    }
}
