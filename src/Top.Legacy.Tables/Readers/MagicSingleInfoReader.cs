using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class MagicSingleInfoReader
    {
        public static MagicSingleInfoRecord Read(TableRow row)
        {
            var record = new MagicSingleInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
            };

            // model count column, superseded by the model list length
            row.Skip();

            record.Models = row.NextStringList();
            record.Velocity = row.NextInt();

            int particleCount = row.NextInt();

            if (particleCount > 0)
            {
                record.Particles = row.NextStringList();

                if (record.Particles.Length > 0)
                {
                    record.DummyIndices = row.NextIntList();
                }
                else
                {
                    // dummy column unused, the parsed particle list came back empty
                    row.Skip();
                }
            }
            else
            {
                // particle and dummy columns unused when the count column is 0
                row.Skip(2);
            }

            record.MotionType = row.NextInt();
            record.LightId = row.NextInt();
            record.ResultParticle = row.NextString();

            return record;
        }
    }
}
