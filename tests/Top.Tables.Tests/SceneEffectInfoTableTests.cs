using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class SceneEffectInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/sceneffectinfo.txt"));
            var table = TableFile.Read<SceneEffectInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("wave1.par"));
            Assert.That(record.DisplayName, Is.EqualTo("Sea Wave"));
            Assert.That(record.EffectType, Is.EqualTo(0));
            Assert.That(record.DummyIds, Is.EqualTo(new[] { -1 }));
            Assert.That(record.Dummy2, Is.EqualTo(-1));
            Assert.That(record.HeightOffset, Is.EqualTo(1));
            Assert.That(record.BaseSize, Is.EqualTo(-1f));
        }

        [Test]
        public void Single_dummy_below_minus_one_clears_the_list()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/sceneffectinfo.txt"));
            var table = TableFile.Read<SceneEffectInfoRecord>(stream);

            var record = table[103];

            Assert.That(record.DummyIds, Is.Empty);
            Assert.That(record.Dummy2, Is.EqualTo(1));
        }
    }
}
