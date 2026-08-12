using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class SelectCharacterTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/selectcha.txt"));
            var table = TableFile.Read<SelectCharacterRecord>(stream);

            var record = table[1];

            Assert.That(record.Bone, Is.EqualTo(0));
            Assert.That(record.Faces, Is.EqualTo(new[] { 255 }));
            Assert.That(record.Hairs, Has.Length.EqualTo(56));
            Assert.That(record.Hairs[0], Is.EqualTo(193));
            Assert.That(record.Hairs[55], Is.EqualTo(248));
            Assert.That(record.Bodies, Is.EqualTo(new[] { 289 }));
            Assert.That(record.Hands, Is.EqualTo(new[] { 465 }));
            Assert.That(record.Feet, Is.EqualTo(new[] { 641 }));
        }
    }
}
