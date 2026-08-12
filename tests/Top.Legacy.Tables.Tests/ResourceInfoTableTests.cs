using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests
{
    public class ResourceInfoTableTests
    {
        [Test]
        public void Reads_known_row_values()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/resourceinfo.txt"));
            var table = TableFile.Read<ResourceInfoRecord>(stream);

            var first = table[1];
            Assert.That(first.Name, Is.EqualTo("sel1.tga"));
            Assert.That(first.Type, Is.EqualTo(ResourceType.Texture));

            var second = table[2];
            Assert.That(second.Name, Is.EqualTo("whitesmoke.par"));
            Assert.That(second.Type, Is.EqualTo(ResourceType.Particle));
        }
    }
}
