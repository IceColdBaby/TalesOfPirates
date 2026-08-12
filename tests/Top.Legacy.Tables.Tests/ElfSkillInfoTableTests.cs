using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ElfSkillInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/elfskillinfo.txt"));
            var table = TableFile.Read<ElfSkillInfoRecord>(stream);

            var record = table[6];

            Assert.That(record.Name, Is.EqualTo("Expert Berserk"));
            Assert.That(record.AbilityType, Is.EqualTo(2));
            Assert.That(record.AbilityIndex, Is.EqualTo(3));
        }
    }
}
