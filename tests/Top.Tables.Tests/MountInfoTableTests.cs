using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class MountInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/mountinfo.txt"));
            var table = TableFile.Read<MountInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Baby Black Dragon Mount"));
            Assert.That(record.ItemId, Is.EqualTo(9500));
            Assert.That(record.BoneId, Is.EqualTo(3));
            Assert.That(record.Heights, Is.EqualTo(new[] { -10, -10, 20, 20 }));
            Assert.That(record.OffsetY, Is.EqualTo(30));
            Assert.That(record.PoseIds, Is.EqualTo(new[] { 30, 30, 16, 16 }));
        }
    }
}
