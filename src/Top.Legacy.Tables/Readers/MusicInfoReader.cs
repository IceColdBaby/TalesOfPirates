using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
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
