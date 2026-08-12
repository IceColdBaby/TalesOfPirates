using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class AreaSetReader
    {
        public static AreaRecord Read(TableRow row)
        {
            return new AreaRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Color = row.NextIntList(),
                Music = row.NextInt(),
                EnvColor = row.NextIntList(),
                LightColor = row.NextIntList(),
                LightDir = row.NextVector3(),
                IsCity = row.NextBool()
            };
        }
    }
}
