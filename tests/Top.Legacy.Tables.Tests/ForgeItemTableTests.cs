using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ForgeItemTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/forgeitem.txt"));
            var table = TableFile.Read<ForgeItemRecord>(stream);

            var record = table[8];

            Assert.That(record.Name, Is.EqualTo("8"));
            Assert.That(record.SuccessRate, Is.EqualTo(45));
            Assert.That(record.Items.Select(x => x.Id), Is.EqualTo(new[] { 1779, 1774, 1804, 0, 0, 0 }));
            Assert.That(record.Items.Select(x => x.Count), Is.EqualTo(new[] { 3, 2, 1, 0, 0, 0 }));
            Assert.That(record.Money, Is.EqualTo(50000));
        }
    }
}
