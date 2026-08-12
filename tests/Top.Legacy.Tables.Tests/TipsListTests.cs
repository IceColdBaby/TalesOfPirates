using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Custom;

namespace Top.Legacy.Tables.Tests
{
    public class TipsListTests
    {
        [Test]
        public void Reads_lines_verbatim()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/tips.tx"));
            var tips = TipsList.Read(stream);

            Assert.That(tips.Lines[0], Does.StartWith("At every new level"));
            Assert.That(tips.Lines[2], Is.EqualTo("You can reset your Stats using Stat Reset."));
        }

        [Test]
        public void Counts_and_keeps_the_final_line_when_the_file_has_no_trailing_newline()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/tips.tx"));
            var tips = TipsList.Read(stream);

            Assert.That(tips.Count, Is.EqualTo(24));
            Assert.That(tips.Lines[23], Is.EqualTo("You can purchase a lot of goodies from our Item Mall!"));
        }
    }
}
