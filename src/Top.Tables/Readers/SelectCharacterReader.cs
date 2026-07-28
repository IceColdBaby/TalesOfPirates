using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class SelectCharacterReader
    {
        public static SelectCharacterRecord Read(TableRow tableRow)
        {
            return new SelectCharacterRecord
            {
                Id = tableRow.NextInt(),
                Name = tableRow.NextString(),
                Bone = tableRow.NextInt(),
                Faces = tableRow.NextIntList(),
                Hairs = tableRow.NextIntList(),
                Bodies = tableRow.NextIntList(),
                Hands = tableRow.NextIntList(),
                Feet = tableRow.NextIntList()
            };
        }
    }
}
