using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class HairsTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/hairs.txt"));
            var table = TableFile.Read<HairRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Hairdo 1"));
            Assert.That(record.Color, Is.EqualTo("Red"));
            Assert.That(record.NeedItems[0], Is.EqualTo(new[] { 1807, 1 }));
            Assert.That(record.NeedItems[2], Is.EqualTo(new[] { 1797, 5 }));
            Assert.That(record.Money, Is.EqualTo(100000));
            Assert.That(record.ItemId, Is.EqualTo(2009));
            Assert.That(record.FailItemIds, Is.EqualTo(new[] { 1999 }));
            Assert.That(record.UsableByCharacter, Is.EqualTo(new[] { true, false, false, false }));
        }
    }
}
