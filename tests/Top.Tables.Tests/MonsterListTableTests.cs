using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class MonsterListTableTests
    {
        [Test]
        public void Reads_monster_list_row()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/monsterlist.txt"));
            var table = TableFile.Read<MonsterListRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Mystic Shrub"));
            Assert.That(record.Area, Is.EqualTo("1"));
            Assert.That(record.X, Is.EqualTo(2092));
            Assert.That(record.Y, Is.EqualTo(2654));
            Assert.That(record.MapName, Is.EqualTo("Ascaron"));
        }
    }
}
