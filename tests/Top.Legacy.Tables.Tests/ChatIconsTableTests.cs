using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ChatIconsTableTests
    {
        private static Table<ChatIconRecord> ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return TableFile.Read<ChatIconRecord>(stream);
        }

        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/chaticons.txt"));
            var table = TableFile.Read<ChatIconRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("1x.tga"));
            Assert.That(record.SmallX, Is.EqualTo(0));
            Assert.That(record.SmallOffIcon, Is.EqualTo("1xh.tga"));
            Assert.That(record.BigIcon, Is.EqualTo("1.tga"));
            Assert.That(record.Hint, Is.EqualTo("0"));
        }

        [Test]
        public void Reads_every_param_column_distinctly()
        {
            var table = ReadText("201\ticon.tga\t11\t22\toff.tga\t33\t44\tbig.tga\t55\t66\t77\n");

            var record = table[201];

            Assert.That(record.SmallX, Is.EqualTo(11));
            Assert.That(record.SmallY, Is.EqualTo(22));
            Assert.That(record.SmallOffIcon, Is.EqualTo("off.tga"));
            Assert.That(record.SmallOffX, Is.EqualTo(33));
            Assert.That(record.SmallOffY, Is.EqualTo(44));
            Assert.That(record.BigIcon, Is.EqualTo("big.tga"));
            Assert.That(record.BigX, Is.EqualTo(55));
            Assert.That(record.BigY, Is.EqualTo(66));
            Assert.That(record.Hint, Is.EqualTo("77"));
        }
    }
}
