using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ShipInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/shipinfo.txt"));
            var table = TableFile.Read<ShipInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Guppy"));
            Assert.That(record.DeedItemId, Is.EqualTo(3988));
            Assert.That(record.CharacterId, Is.EqualTo(305));
            Assert.That(record.PoseId, Is.EqualTo(405));
            Assert.That(record.HullId, Is.EqualTo(43));
            Assert.That(record.EngineOptions, Is.EqualTo(new[] { 15, 16 }));
            Assert.That(record.HeadOptions, Is.EqualTo(new[] { 8, 9, 10 }));
            Assert.That(record.CannonOptions, Is.EqualTo(new[] { 53, 54, 55 }));
            Assert.That(record.EquipmentOptions, Is.EqualTo(new[] { 73, 74 }));
            Assert.That(record.LevelLimit, Is.EqualTo(15));
            Assert.That(record.ProfessionLimits, Is.EqualTo(new[] { -1 }));
            Assert.That(record.Endurance, Is.EqualTo(0));
            Assert.That(record.EnduranceRecovery, Is.EqualTo(0));
            Assert.That(record.Defence, Is.EqualTo(0));
            Assert.That(record.Resist, Is.EqualTo(0));
            Assert.That(record.MinAttack, Is.EqualTo(0));
            Assert.That(record.MaxAttack, Is.EqualTo(0));
            Assert.That(record.AttackDistance, Is.EqualTo(0));
            Assert.That(record.ReloadTime, Is.EqualTo(0));
            Assert.That(record.SplashScope, Is.EqualTo(0));
            Assert.That(record.Capacity, Is.EqualTo(24));
            Assert.That(record.Supply, Is.EqualTo(0));
            Assert.That(record.SupplyConsume, Is.EqualTo(0));
            Assert.That(record.CannonSpeed, Is.EqualTo(0));
            Assert.That(record.MoveSpeed, Is.EqualTo(0));
            Assert.That(record.Description, Is.EqualTo("0"));
            Assert.That(record.Param, Is.EqualTo(0));
        }
    }
}
