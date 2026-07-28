using NUnit.Framework;

namespace Top.Tables.Tests
{
    public class TableFormatExceptionTests
    {
        [Test]
        public void Message_includes_file_and_line()
        {
            var exception = new TableFormatException("table/iteminfo.txt", 12, "field count changed");

            Assert.That(exception.File, Is.EqualTo("table/iteminfo.txt"));
            Assert.That(exception.Line, Is.EqualTo(12));
            Assert.That(exception.Message, Is.EqualTo("table/iteminfo.txt:12: field count changed"));
        }

        [Test]
        public void Message_without_file_names_only_the_line()
        {
            var exception = new TableFormatException(null, 3, "field count changed");

            Assert.That(exception.Message, Is.EqualTo("line 3: field count changed"));
        }
    }
}
