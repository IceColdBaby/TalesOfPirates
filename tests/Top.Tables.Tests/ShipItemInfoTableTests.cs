using System.IO;
using NUnit.Framework;
using Top.Tables.Records;

namespace Top.Tables.Tests
{
    public class ShipItemInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/shipiteminfo.txt"));
            var table = TableFile.Read<ShipItemInfoRecord>(stream);

            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Titanic Hull"));
            Assert.That(record.ModelId, Is.EqualTo(2000000000));
            Assert.That(record.Motors, Is.EqualTo(new[] { 0, 0, 0, 0 }));
            Assert.That(record.Price, Is.EqualTo(31237));
            Assert.That(record.Endurance, Is.EqualTo(4286));
            Assert.That(record.EnduranceRecovery, Is.EqualTo(0));
            Assert.That(record.Defence, Is.EqualTo(78));
            Assert.That(record.Resist, Is.EqualTo(20));
            Assert.That(record.MinAttack, Is.EqualTo(0));
            Assert.That(record.MaxAttack, Is.EqualTo(0));
            Assert.That(record.AttackDistance, Is.EqualTo(0));
            Assert.That(record.ReloadTime, Is.EqualTo(0));
            Assert.That(record.SplashScope, Is.EqualTo(0));
            Assert.That(record.Capacity, Is.EqualTo(0));
            Assert.That(record.Supply, Is.EqualTo(200));
            Assert.That(record.SupplyConsume, Is.EqualTo(2));
            Assert.That(record.CannonSpeed, Is.EqualTo(0));
            Assert.That(record.MoveSpeed, Is.EqualTo(0));
            Assert.That(record.Description, Is.EqualTo("0"));
            Assert.That(record.Param, Is.EqualTo(0));
        }
    }
}
