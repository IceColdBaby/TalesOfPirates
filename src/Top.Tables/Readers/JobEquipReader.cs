using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class JobEquipReader
    {
        public static JobEquipRecord Read(TableRow row)
        {
            return new JobEquipRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                ItemIds = row.NextIntList(),
            };
        }
    }
}
