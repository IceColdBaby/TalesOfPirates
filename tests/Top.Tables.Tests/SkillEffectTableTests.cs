using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class SkillEffectTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/skilleff.txt"));
            var table = TableFile.Read<SkillEffectRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Burn"));
            Assert.That(record.Frequency, Is.EqualTo(1));
            Assert.That(record.OnTransferScript, Is.EqualTo("State_Rs_Tran"));
            Assert.That(record.AddType, Is.EqualTo(1));
            Assert.That(record.CanCancel, Is.False);
            Assert.That(record.AreaEffect, Is.EqualTo(112));
            Assert.That(record.Effect, Is.EqualTo(115));
            Assert.That(record.LevelIcons[0], Is.EqualTo("e0013"));
            Assert.That(record.Color, Is.EqualTo(15218966));
        }
    }
}
