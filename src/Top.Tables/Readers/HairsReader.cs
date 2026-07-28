using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class HairsReader
    {
        public static HairRecord Read(TableRow row)
        {
            var record = new HairRecord();

            record.Id = row.NextInt();
            record.Name = row.NextString();
            record.Color = row.NextString();
            record.NeedItems = new int[4][];

            for (int i = 0; i < 4; i++)
            {
                record.NeedItems[i] = row.NextIntList();
            }

            record.Money = row.NextInt();
            record.ItemId = row.NextInt();
            record.FailItemIds = row.NextIntList();
            record.UsableByCharacter = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                record.UsableByCharacter[i] = row.NextBool();
            }

            record.ItemListId = row.NextInt();
            record.ModelDenominate = row.NextString();

            return record;
        }
    }
}
