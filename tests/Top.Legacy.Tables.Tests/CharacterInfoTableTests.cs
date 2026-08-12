using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class CharacterInfoTableTests
    {
        private static Table<CharacterInfoRecord> ReadFixture()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/characterinfo.txt"));
            return TableFile.Read<CharacterInfoRecord>(stream);
        }

        [Test]
        public void Reads_known_row_values()
        {
            var table = ReadFixture();
            var record = table[1];

            Assert.That(record.Name, Is.EqualTo("Long Haired Guy"));
            Assert.That(record.ModalType, Is.EqualTo(CharacterModalType.MainCharacter));
            Assert.That(record.ControlType, Is.EqualTo(CharacterControlType.Player));
            Assert.That(record.Model, Is.EqualTo(0));
            Assert.That(record.ActionId, Is.EqualTo(1));
            Assert.That(record.Parts, Is.EqualTo(new[] { 2000, 255, 464, 640, 816, 0, 0, 0 }));
            Assert.That(record.EquipItemTypes, Is.EqualTo(new[] { 1, 2, 3, 4, 7, 8 }));
            Assert.That(record.Height, Is.EqualTo(2.599f).Within(1e-5f));
            Assert.That(record.MoveSpeed, Is.EqualTo(480));
            Assert.That(record.Scaling, Is.EqualTo(new Vector3(1f, 1f, 1f)));
        }

        [Test]
        public void Framework_maps_to_action_id_for_the_rig_converter()
        {
            var table = ReadFixture();

            CharacterInfoRecord match = table.FirstOrDefault(record => record.Model == 1);

            Assert.That(match, Is.Not.Null);
            Assert.That(match.ActionId, Is.EqualTo(2));
        }
    }
}
