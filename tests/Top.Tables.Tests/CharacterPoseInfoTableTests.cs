using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class CharacterPoseInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/characterposeinfo.txt"));
            var table = TableFile.Read<CharacterPoseInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Normal Wait"));
            Assert.That(record.RealPoseIds, Is.EqualTo(new[] { 1, 55, 109, 163, 217, 271, 325 }));
        }
    }
}
