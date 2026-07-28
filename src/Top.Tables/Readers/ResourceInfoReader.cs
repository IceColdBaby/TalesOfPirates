using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class ResourceInfoReader
    {
        public static ResourceInfoRecord Read(TableRow row)
        {
            return new ResourceInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Type = row.NextEnum<ResourceType>()
            };
        }
    }
}
