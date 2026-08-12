using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class ItemRefineInfoReader
    {
        public static ItemRefineInfoRecord Read(TableRow row)
        {
            return new ItemRefineInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                EffectIds = row.NextInts(14),
                CharacterEffectScales = row.NextFloats(4)
            };
        }
    }
}
