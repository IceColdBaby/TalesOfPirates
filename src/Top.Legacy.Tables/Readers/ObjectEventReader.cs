using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ObjectEventReader
    {
        public static ObjectEventRecord Read(TableRow row)
        {
            return new ObjectEventRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                EventType = row.NextEnum<EventType>(),
                ArouseType = row.NextEnum<EventArouseType>(),
                ArouseRadius = row.NextInt(),
                Effect = row.NextInt(),
                Music = row.NextInt(),
                BornEffect = row.NextInt(),
                Cursor = row.NextInt(),
                MainCharacterType = row.NextInt()
            };
        }
    }
}
