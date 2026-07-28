using NUnit.Framework;

namespace Top.Tables.Tests
{
    public class TableTextTests
    {
        [Test]
        public void SplitFields_collapses_interior_runs()
        {
            Assert.That(TableText.SplitFields("a\t\tb", '\t'), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void SplitFields_keeps_one_empty_leading_field()
        {
            Assert.That(TableText.SplitFields("\ta\tb", '\t'), Is.EqualTo(new[] { "", "a", "b" }));
        }

        [Test]
        public void SplitFields_drops_trailing_separators()
        {
            Assert.That(TableText.SplitFields("a\tb\t\t", '\t'), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void SplitFields_returns_single_char_line_whole()
        {
            Assert.That(TableText.SplitFields("\t", '\t'), Is.EqualTo(new[] { "\t" }));
        }

        [Test]
        public void SplitFields_empty_text_yields_no_fields()
        {
            Assert.That(TableText.SplitFields("", '\t'), Is.Empty);
        }
    }
}
