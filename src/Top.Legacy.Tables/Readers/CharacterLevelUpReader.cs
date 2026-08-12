using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class CharacterLevelUpReader
    {
        public static CharacterLevelUpRecord Read(TableRow row)
        {
            return new CharacterLevelUpRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Exp = row.NextLong(),
            };
        }
    }
}
