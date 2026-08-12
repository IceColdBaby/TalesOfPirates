using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class MusicInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/musicinfo.txt"));
            var table = TableFile.Read<MusicInfoRecord>(stream);

            Assert.That(table[21].Name, Is.EqualTo("music/sound/pc/01.wav"));
            Assert.That(table[21].IsSound, Is.True);
        }
    }
}
