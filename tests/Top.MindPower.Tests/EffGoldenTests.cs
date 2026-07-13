using System.IO;
using NUnit.Framework;
using Top.MindPower.Effects;

namespace Top.MindPower.Tests
{
    public class EffGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // lighty.eff: version 2, a single built-in Model sub-effect (RectPlane + 2.tga texture),
            // standard alpha blend (D3DBLEND_SRCALPHA=5 / D3DBLEND_INVSRCALPHA=7). No path/sound.
            using var stream = File.OpenRead(Fixtures.Path("eff/lighty.eff"));
            var file = EffFile.Read(stream);

            Assert.That(file.Version, Is.EqualTo(2u));
            Assert.That(file.TechniqueIndex, Is.EqualTo(0));
            Assert.That(file.UsePath, Is.EqualTo(0));
            Assert.That(file.UseSound, Is.EqualTo(0));
            Assert.That(file.Rotating, Is.EqualTo(0));
            Assert.That(file.RotationVelocity, Is.EqualTo(1f));
            Assert.That(file.Effects.Length, Is.EqualTo(1));

            var e0 = file.Effects[0];
            Assert.That(e0.Name, Is.EqualTo("lighty.eff"));
            Assert.That(e0.EffectType, Is.EqualTo(EffectType.Model));
            Assert.That(e0.SourceBlend, Is.EqualTo(5));
            Assert.That(e0.DestinationBlend, Is.EqualTo(7));
            Assert.That(e0.FrameTime.Length, Is.EqualTo(1));
            Assert.That(e0.ModelName, Is.EqualTo("RectPlane"));
            Assert.That(e0.TextureName, Is.EqualTo("2.tga"));
            Assert.That(e0.Billboard, Is.EqualTo(1));

            // version-2 file: the v3+ texture-frame block is absent, so the name list is null.
            Assert.That(e0.TextureFrameNames, Is.Null);
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("eff/lighty.eff", EffFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("eff", "*.eff",
                EffFile.Read, (s, f) => f.Write(s));
        }
    }
}
