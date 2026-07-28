using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class EventSoundReader
    {
        public static EventSoundRecord Read(TableRow row)
        {
            return new EventSoundRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                SoundId = row.NextInt()
            };
        }
    }
}
