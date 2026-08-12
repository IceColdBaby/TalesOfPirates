using System.Numerics;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class MapInfoReader
    {
        public static MapInfoRecord Read(TableRow row)
        {
            var record = new MapInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                DisplayName = row.NextString(),
                ShowSwitch = row.NextBool()
            };

            int[] init = row.NextIntList();

            if (init.Length >= 2)
            {
                record.InitX = init[0];
                record.InitY = init[1];
            }

            var lightDirectionInts = row.NextIntList();

            if (lightDirectionInts.Length >= 3)
            {
                record.LightDirection = new Vector3(
                    lightDirectionInts.Length > 0 ? lightDirectionInts[0] / 255f : 1f,
                    lightDirectionInts.Length > 1 ? lightDirectionInts[1] / 255f : 1f,
                    lightDirectionInts.Length > 2 ? lightDirectionInts[2] / 255f : -1f
                );
            }

            var colorInts = row.NextIntList();

            if (colorInts.Length >= 3)
            {
                record.LightColor = new Vector3(
                    colorInts.Length > 0 ? colorInts[0] / 255f : 0f,
                    colorInts.Length > 1 ? colorInts[1] / 255f : 0f,
                    colorInts.Length > 2 ? colorInts[2] / 255f : 0f
                );
            }

            return record;
        }
    }
}
