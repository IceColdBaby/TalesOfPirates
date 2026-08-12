using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class MountInfoReader
    {
        public static MountInfoRecord Read(TableRow row)
        {
            return new MountInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                ItemId = row.NextInt(),
                BoneId = row.NextInt(),
                Heights = row.NextIntList(),
                OffsetX = row.NextInt(),
                OffsetY = row.NextInt(),
                PoseIds = row.NextIntList()
            };
        }
    }
}
