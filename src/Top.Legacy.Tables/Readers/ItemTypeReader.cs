using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ItemTypeReader
    {
        public static ItemTypeRecord Read(TableRow row)
        {
            return new ItemTypeRecord
            {
                Id = row.NextInt(),
                Name = row.NextString()
            };
        }
    }
}
