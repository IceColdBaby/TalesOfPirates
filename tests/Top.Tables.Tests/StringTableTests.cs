using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Tables.Custom;

namespace Top.Tables.Tests
{
    public class StringTableTests
    {
        private static StringTable ReadFixture()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/StringSet.txt"));
            return StringTable.Read(stream);
        }

        private static StringTable ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return StringTable.Read(stream);
        }

        [Test]
        public void Looks_up_known_strings()
        {
            var table = ReadFixture();

            Assert.That(table.GetString(0), Is.EqualTo("english"));
            Assert.That(table.GetString(12), Is.EqualTo("Character"));
        }

        [Test]
        public void Escapes_expand_to_control_characters()
        {
            var table = ReadFixture();

            Assert.That(table.GetString(1), Is.EqualTo("msg unable to create item effect ID = %d\n"));
        }

        [Test]
        public void Missing_id_yields_empty_string()
        {
            Assert.That(ReadFixture().GetString(99999), Is.EqualTo(""));
        }

        [Test]
        public void Escaped_tab_expands_but_the_split_uses_the_real_tab()
        {
            var table = ReadText("[500]\t\"before\\tafter\"");

            Assert.That(table.GetString(500), Is.EqualTo("before\tafter"));
        }

        [Test]
        public void Escaped_tab_as_the_only_separator_still_splits_after_expansion()
        {
            var table = ReadText("[501]\\t\"hello\"");

            Assert.That(table.GetString(501), Is.EqualTo("hello"));
        }

        [Test]
        public void Last_duplicate_id_wins()
        {
            var table = ReadText("[7]\t\"first\"\n[7]\t\"second\"");

            Assert.That(table.GetString(7), Is.EqualTo("second"));
        }
    }
}
