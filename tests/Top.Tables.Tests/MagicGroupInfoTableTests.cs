using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class MagicGroupInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/magicgroupinfo.txt"));
            var table = TableFile.Read<MagicGroupInfoRecord>(stream);

            var record = table[2];

            Assert.That(record.EffectIds, Is.EqualTo(new[] { 10, 25, 26 }));
            Assert.That(record.Counts, Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(record.TotalCount, Is.EqualTo(3));
            Assert.That(record.EmissionType, Is.EqualTo(1));
        }
    }
}
