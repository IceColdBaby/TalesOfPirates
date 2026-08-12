using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class SceneEffectInfoReader
    {
        public static SceneEffectInfoRecord Read(TableRow row)
        {
            var record = new SceneEffectInfoRecord();

            record.Id = row.NextInt();
            record.Name = row.NextString();
            record.DisplayName = row.NextString();
            record.PhotoName = row.NextString();
            record.EffectType = row.NextInt();
            record.ObjectType = row.NextInt();
            record.DummyIds = row.NextIntList();

            if (record.DummyIds.Length == 1 && record.DummyIds[0] < -1)
            {
                record.DummyIds = System.Array.Empty<int>();
            }

            record.Dummy2 = row.NextInt();
            record.HeightOffset = row.NextInt();
            record.PlayTime = row.NextFloat();
            record.LightId = row.NextInt();
            record.BaseSize = row.NextFloat();

            return record;
        }
    }
}
