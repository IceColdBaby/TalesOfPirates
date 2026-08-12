using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class SkillInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/skillinfo.txt"));
            var table = TableFile.Read<SkillInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Windslash"));
            Assert.That(record.FightType, Is.EqualTo(SkillFightType.Fight));
            Assert.That(record.Phase, Is.EqualTo(2));
            Assert.That(record.ApplyDistance, Is.EqualTo(400));
            Assert.That(record.ApplyTarget, Is.EqualTo(SkillApplyTarget.Enemy));
            Assert.That(record.SelectTarget, Is.EqualTo(SkillSelectTarget.Enemy));
            Assert.That(record.CooldownScript, Is.EqualTo("1000"));
            Assert.That(record.AttackSound, Is.EqualTo(105));
            Assert.That(record.HitSound, Is.EqualTo(129));
            Assert.That(record.ActionPoses, Is.EqualTo(new[] { 7 }));
            Assert.That(record.PoseCount, Is.EqualTo(1));
            Assert.That(record.TargetDummyLink, Is.EqualTo(2));
            Assert.That(record.TargetEffectId, Is.EqualTo(101));
            Assert.That(record.TargetEffectTime, Is.EqualTo(0));
            Assert.That(record.AgroundEffectId, Is.EqualTo(0));
            Assert.That(record.WaterEffectId, Is.EqualTo(0));
            Assert.That(record.Icon, Is.EqualTo("0"));
            Assert.That(record.PlayCount, Is.EqualTo(0));
            Assert.That(record.Operations, Is.EqualTo(new[] { 0 }));
            Assert.That(record.DescribeHint, Is.EqualTo("Attacks target with up to 300% bonus damage"));
            Assert.That(record.EffectHint, Is.EqualTo("0"));
            Assert.That(record.ExpendHint, Is.EqualTo("0"));
        }
    }
}
