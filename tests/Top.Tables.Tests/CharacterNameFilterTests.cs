using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Tables.Custom;

namespace Top.Tables.Tests
{
    public class CharacterNameFilterTests
    {
        private static CharacterNameFilter ReadFixture()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/ChaNameFilter.txt"));
            return CharacterNameFilter.Read(stream);
        }

        private static CharacterNameFilter ReadText(string text)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(text));
            return CharacterNameFilter.Read(stream);
        }

        [Test]
        public void Rejects_names_containing_entries()
        {
            var filter = ReadFixture();

            Assert.That(filter.Entries[0], Is.EqualTo("GM"));
            Assert.That(filter.IsAllowed("GM"), Is.False);
            Assert.That(filter.IsAllowed("MyTestChar"), Is.False);
            Assert.That(filter.IsAllowed("Alice"), Is.True);
        }

        [Test]
        public void Case_differing_name_is_allowed()
        {
            var filter = ReadFixture();

            Assert.That(filter.IsAllowed("cs"), Is.True);
        }

        [Test]
        public void Entries_are_stored_verbatim_including_blank_and_comment_like_lines()
        {
            var filter = ReadText("  padded entry  \n//not a comment\n\nlast entry\n");

            Assert.That(filter.Entries, Is.EqualTo(new[]
            {
                "  padded entry  ",
                "//not a comment",
                "",
                "last entry",
            }));
        }
    }
}
