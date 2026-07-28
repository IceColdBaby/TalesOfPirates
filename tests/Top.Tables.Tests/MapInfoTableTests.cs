using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class MapInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/mapinfo.txt"));
            var table = TableFile.Read<MapInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("garner"));
            Assert.That(record.DisplayName, Is.EqualTo("Ascaron"));
            Assert.That(record.ShowSwitch, Is.True);
            Assert.That(record.InitX, Is.EqualTo(2202));
            Assert.That(record.InitY, Is.EqualTo(2782));
        }
    }
}
