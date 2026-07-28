using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class ItemRefineEffectInfoReader
    {
        public static ItemRefineEffectInfoRecord Read(TableRow row)
        {
            var record = new ItemRefineEffectInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                LightId = row.NextInt(),
                EffectIds = new int[4][],
                EffectDummies = new int[4],
                CharacterEffectCounts = new int[4]
            };

            for (int slot = 0; slot < 4; slot++)
            {
                record.EffectIds[slot] = row.NextInts(4);
                record.EffectDummies[slot] = row.NextInt();

                for (int character = 0; character < 4; character++)
                {
                    if (record.EffectIds[slot][character] != 0)
                    {
                        record.CharacterEffectCounts[character]++;
                    }
                }
            }

            return record;
        }
    }
}
