using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class LifeLevelUpTableTests
    {
        [Test]
        public void Reads_life_level_curve()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/lifelvup.txt"));
            var table = TableFile.Read<LifeLevelUpRecord>(stream);

            Assert.That(table[2].Exp, Is.EqualTo(1500));
            Assert.That(table[10].Exp, Is.EqualTo(85500));
        }
    }
}
