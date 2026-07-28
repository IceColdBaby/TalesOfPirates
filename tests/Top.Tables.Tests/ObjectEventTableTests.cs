using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class ObjectEventTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/objevent.txt"));
            var table = TableFile.Read<ObjectEventRecord>(stream);

            var record = table[1];

            Assert.That(record.EventType, Is.EqualTo(EventType.Action));
            Assert.That(record.ArouseType, Is.EqualTo(EventArouseType.Click));
            Assert.That(record.ArouseRadius, Is.EqualTo(200));
            Assert.That(record.Effect, Is.EqualTo(399));
            Assert.That(record.Music, Is.EqualTo(147));
            Assert.That(record.BornEffect, Is.EqualTo(468));
            Assert.That(record.Cursor, Is.EqualTo(9));
            Assert.That(record.MainCharacterType, Is.EqualTo(0));
        }

        [Test]
        public void Reads_main_character_type()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/objevent.txt"));
            var table = TableFile.Read<ObjectEventRecord>(stream);

            Assert.That(table[2].MainCharacterType, Is.EqualTo(2));
            Assert.That(table[4].MainCharacterType, Is.EqualTo(2));
        }
    }
}
