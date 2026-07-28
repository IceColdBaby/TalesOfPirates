using System.Globalization;
using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class StoneInfoReader
    {
        public static StoneInfoRecord Read(TableRow row)
        {
            var record = new StoneInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                ItemId = row.NextInt(),
                EquipPositions = row.NextIntList(),
                Type = row.NextInt(),
                HintFunction = row.NextString()
            };

            int.TryParse(row.NextString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out record.ItemRgb);

            return record;
        }
    }
}
