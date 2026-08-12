using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class CharacterPoseInfoReader
    {
        public static CharacterPoseInfoRecord Read(TableRow row)
        {
            return new CharacterPoseInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                RealPoseIds = row.NextInts(7)
            };
        }
    }
}
