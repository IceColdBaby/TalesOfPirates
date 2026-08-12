using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class MagicSingleInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/magicsingleinfo.txt"));
            var table = TableFile.Read<MagicSingleInfoRecord>(stream);

            var record = table[20];

            Assert.That(record.Models, Is.EqualTo(new[] { "icearrowf.eff", "icearrowf(a).eff" }));
            Assert.That(record.Velocity, Is.EqualTo(10));
            Assert.That(record.Particles, Is.EqualTo(new[] { "icearrowf" }));
            Assert.That(record.DummyIndices, Is.EqualTo(new[] { -1 }));
            Assert.That(record.MotionType, Is.EqualTo(2));
            Assert.That(record.ResultParticle, Is.EqualTo("icearrowh"));
        }

        [Test]
        public void Skips_particle_and_dummy_columns_when_count_is_zero()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/magicsingleinfo.txt"));
            var table = TableFile.Read<MagicSingleInfoRecord>(stream);

            var record = table[11];

            Assert.That(record.Particles, Is.Empty);
            Assert.That(record.DummyIndices, Is.Empty);
            Assert.That(record.MotionType, Is.EqualTo(6));
            Assert.That(record.LightId, Is.EqualTo(0));
            Assert.That(record.ResultParticle, Is.EqualTo("0"));
        }
    }
}
