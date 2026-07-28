using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class ItemRefineEffectInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/itemrefineeffectinfo.txt"));
            var table = TableFile.Read<ItemRefineEffectInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Staff Bonus"));
            Assert.That(record.LightId, Is.EqualTo(3));
            Assert.That(record.EffectIds[0], Is.EqualTo(new[] { 0, 0, 300, 300 }));
            Assert.That(record.EffectIds[1], Is.EqualTo(new[] { 0, 0, 301, 301 }));
            Assert.That(record.EffectIds[2], Is.EqualTo(new[] { 0, 0, 302, 302 }));
            Assert.That(record.EffectIds[3], Is.EqualTo(new[] { 0, 0, 0, 0 }));
            Assert.That(record.EffectDummies, Is.EqualTo(new[] { 4, 4, 0, 0 }));
            Assert.That(record.CharacterEffectCounts, Is.EqualTo(new[] { 0, 0, 3, 3 }));
        }
    }
}
