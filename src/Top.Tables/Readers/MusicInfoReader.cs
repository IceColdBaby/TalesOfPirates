using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class MusicInfoReader
    {
        public static MusicInfoRecord Read(TableRow row)
        {
            return new MusicInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                IsSound = row.NextBool()
            };
        }
    }
}
