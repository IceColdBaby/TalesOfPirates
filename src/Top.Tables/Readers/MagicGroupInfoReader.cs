using System.Linq;
using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class MagicGroupInfoReader
    {
        public static MagicGroupInfoRecord Read(TableRow row)
        {
            var record = new MagicGroupInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString()
            };

            int typeCount = row.NextInt();

            if (typeCount > 0)
            {
                record.EffectIds = row.NextIntList();
                if (record.EffectIds.Length > 0)
                {
                    record.Counts = row.NextIntList();
                }
                else
                {
                    // quantity column unused, the parsed element id list came back empty
                    row.Skip();
                }
            }
            else
            {
                // element and quantity columns unused when the count column is 0
                row.Skip(2);
            }

            record.EmissionType = row.NextInt();

            int total = record.Counts.Sum();

            record.TotalCount = total;

            return record;
        }
    }
}
