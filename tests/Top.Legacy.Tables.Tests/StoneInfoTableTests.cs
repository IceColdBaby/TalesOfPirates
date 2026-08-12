using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class StoneInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/stoneinfo.txt"));
            var table = TableFile.Read<StoneInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Fiery Gem"));
            Assert.That(record.ItemId, Is.EqualTo(878));
            Assert.That(record.EquipPositions, Is.EqualTo(new[] { 1 }));
            Assert.That(record.Type, Is.EqualTo(1));
            Assert.That(record.HintFunction, Is.EqualTo("ItemHint_LieYanS"));
            Assert.That(record.ItemRgb, Is.EqualTo(0xF6D243));
        }
    }
}
