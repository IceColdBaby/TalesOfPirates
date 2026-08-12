using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class CharacterLevelUpTableTests
    {
        [Test]
        public void Reads_character_level_curve()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/character_lvup.txt"));

            var table = TableFile.Read<CharacterLevelUpRecord>(stream);

            Assert.That(table[2].Exp, Is.EqualTo(5L));
            Assert.That(table[10].Exp, Is.EqualTo(3208L));
            Assert.That(table[15].Exp, Is.EqualTo(21210L));
        }
    }
}
