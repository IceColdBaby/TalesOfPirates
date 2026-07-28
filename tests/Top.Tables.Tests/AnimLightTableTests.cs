using System.IO;
using NUnit.Framework;
using Top.Tables.Custom;

namespace Top.Tables.Tests
{
    public class AnimLightTableTests
    {
        [Test]
        public void Reads_groups_and_normalized_colors()
        {
            using var stream = File.OpenRead(Fixtures.Path("tables/aaa.tx"));
            var table = AnimLightTable.Read(stream);

            Assert.That(table.Groups, Has.Count.EqualTo(9));
            Assert.That(table.Groups[0].Keyframes, Has.Count.EqualTo(6));

            var key = table.Groups[0].Keyframes[0];
            Assert.That(key.Id, Is.EqualTo(0));
            Assert.That(key.LightType, Is.EqualTo(1));
            Assert.That(key.Ambient[0], Is.EqualTo(134f / 255f).Within(1e-5f));
            Assert.That(key.Ambient[1], Is.EqualTo(85f / 255f).Within(1e-5f));
            Assert.That(key.Ambient[2], Is.EqualTo(247f / 255f).Within(1e-5f));
            Assert.That(key.Range, Is.EqualTo(10f));
            Assert.That(key.Attenuation1, Is.EqualTo(0.6f).Within(1e-5f));

            Assert.That(table.Groups[1].Keyframes, Has.Count.EqualTo(4));
        }
    }
}
