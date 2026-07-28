using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class ItemRefineInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/itemrefineinfo.txt"));
            var table = TableFile.Read<ItemRefineInfoRecord>(stream);

            var record = table[4];

            Assert.That(record.Name, Is.EqualTo("Serpentine Sword"));
            Assert.That(record.EffectIds, Is.EqualTo(new[]
            {
                3, 9, 0, 12, 26, 0, 25, 0, 24, 0, 0, 27, 0, 0
            }));
            Assert.That(record.CharacterEffectScales, Is.EqualTo(new[] { 0.6f, 1f, 1f, 0.7f }));
        }
    }
}
