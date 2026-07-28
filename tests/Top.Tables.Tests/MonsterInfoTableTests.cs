using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class MonsterInfoTableTests
    {
        private static Table<MonsterInfoRecord> ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return TableFile.Read<MonsterInfoRecord>(stream);
        }

        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/monsterinfo.txt"));
            var table = TableFile.Read<MonsterInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.StartX, Is.EqualTo(2150));
            Assert.That(record.StartY, Is.EqualTo(2528));
            Assert.That(record.EndX, Is.EqualTo(2290));
            Assert.That(record.EndY, Is.EqualTo(2676));
            Assert.That(record.MonsterIds, Is.EqualTo(new[] { 185, 75 }));
            Assert.That(record.MapName, Is.EqualTo("garner"));
        }

        [Test]
        public void Reads_coordinate_cell_with_extra_trailing_component()
        {
            var table = ReadText("201\tsynthetic\t215000,252800,999\t229000,267600,999\t7,8\ttest\n");

            var record = table[201];

            Assert.That(record.StartX, Is.EqualTo(2150));
            Assert.That(record.StartY, Is.EqualTo(2528));
            Assert.That(record.EndX, Is.EqualTo(2290));
            Assert.That(record.EndY, Is.EqualTo(2676));
        }
    }
}
