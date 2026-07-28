using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class ChatIconsReader
    {
        public static ChatIconRecord Read(TableRow row)
        {
            return new ChatIconRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                SmallX = row.NextInt(),
                SmallY = row.NextInt(),
                SmallOffIcon = row.NextString(),
                SmallOffX = row.NextInt(),
                SmallOffY = row.NextInt(),
                BigIcon = row.NextString(),
                BigX = row.NextInt(),
                BigY = row.NextInt(),
                Hint = row.NextString()
            };
        }
    }
}
