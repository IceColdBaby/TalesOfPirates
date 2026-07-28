using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class ItemPreTableTests
    {
        [Test]
        public void Names_keep_trailing_spaces()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/itempre.txt"));
            Table<ItemPreRecord> table = TableFile.Read<ItemPreRecord>(stream);

            Assert.That(table[1].Name, Is.EqualTo("Mammoth "));
        }
    }
}
