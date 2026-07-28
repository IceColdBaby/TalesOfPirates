using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class NotifySetTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/notifyset.txt"));
            var table = TableFile.Read<NotifyRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Skill Learning Detection"));
            Assert.That(record.Type, Is.EqualTo(1));
            Assert.That(record.Message, Is.EqualTo("Insufficient EXP, unable to learn"));
        }
    }
}
