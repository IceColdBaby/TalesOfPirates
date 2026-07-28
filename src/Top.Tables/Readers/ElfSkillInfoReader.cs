using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class ElfSkillInfoReader
    {
        public static ElfSkillInfoRecord Read(TableRow row)
        {
            return new ElfSkillInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                AbilityType = row.NextInt(),
                AbilityIndex = row.NextInt()
            };
        }
    }
}
