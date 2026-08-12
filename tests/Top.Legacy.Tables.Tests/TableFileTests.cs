using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class TableFileTests
    {
        private static Table<ChatIconRecord> ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return TableFile.Read<ChatIconRecord>(stream);
        }

        [Test]
        public void Trailing_comment_is_trimmed_and_the_row_kept()
        {
            var table = ReadText("1\tsmile\t2\t3\ts.png\t4\t5\tb.png\t6\t7\thint\t//tuned\n");

            Assert.That(table.TryGetById(1, out var record), Is.True);
            Assert.That(record.Name, Is.EqualTo("smile"));
            Assert.That(record.BigY, Is.EqualTo(7));
            Assert.That(record.Hint, Is.EqualTo("hint"));
        }

        [Test]
        public void Full_comment_lines_are_skipped()
        {
            var table = ReadText("//id\tname\theader\n1\tsmile\t2\t3\ts.png\t4\t5\tb.png\t6\t7\thint\n");

            Assert.That(table.Count, Is.EqualTo(1));
        }
    }
}
