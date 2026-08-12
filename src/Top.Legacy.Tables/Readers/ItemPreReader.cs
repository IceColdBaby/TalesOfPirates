using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ItemPreReader
    {
        public static ItemPreRecord Read(TableRow row)
        {
            return new ItemPreRecord
            {
                Id = row.NextInt(),
                Name = row.NextString()
            };
        }
    }
}
