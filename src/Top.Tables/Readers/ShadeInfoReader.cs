using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class ShadeInfoReader
    {
        public static ShadeInfoRecord Read(TableRow row)
        {
            return new ShadeInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                DisplayName = row.NextString(),
                Size = row.NextFloat(),
                Animated = row.NextBool(),
                Rows = row.NextInt(),
                Cols = row.NextInt(),
                UseAlphaTest = row.NextBool(),
                AlphaType = row.NextInt(),
                Color = row.NextInts(4),
                Type = row.NextInt()
            };
        }
    }
}
