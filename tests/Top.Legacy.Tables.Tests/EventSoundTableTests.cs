using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class EventSoundTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/eventsound.txt"));
            Table<EventSoundRecord> table = TableFile.Read<EventSoundRecord>(stream);

            Assert.That(table[1].Name, Is.EqualTo("Mouse Click"));
            Assert.That(table[1].SoundId, Is.EqualTo(92));
        }
    }
}
