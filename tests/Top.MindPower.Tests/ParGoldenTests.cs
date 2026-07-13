using System.IO;
using NUnit.Framework;
using Top.MindPower.Particles;

namespace Top.MindPower.Tests
{
    public class ParGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            // 01030010.par: version 7, no emitters, one strip block (v7+ container path),
            // no model block (model_num requires v8+). Strip is a white (1,1,1,1) ribbon
            // textured with eff0114.tga, standard alpha blend (D3DBLEND_SRCALPHA=5 / D3DBLEND_ONE=2).
            using var stream = File.OpenRead(Fixtures.Path("par/01030010.par"));
            var file = ParFile.Read(stream);

            Assert.That(file.Version, Is.EqualTo(7u));
            Assert.That(file.PartName, Is.EqualTo("01030010"));
            Assert.That(file.Length, Is.EqualTo(0f));
            Assert.That(file.Emitters.Length, Is.EqualTo(0));
            Assert.That(file.Strips.Length, Is.EqualTo(1));

            // v7 has no model block at all, so Models stays null.
            Assert.That(file.Models, Is.Null);

            var strip = file.Strips[0];
            Assert.That(strip.MaxLength, Is.EqualTo(100));
            Assert.That(strip.Dummy, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(strip.Color.R, Is.EqualTo(1f));
            Assert.That(strip.Color.A, Is.EqualTo(1f));
            Assert.That(strip.Life, Is.EqualTo(0.5f));
            Assert.That(strip.Step, Is.EqualTo(0.03f));
            Assert.That(strip.TextureName, Is.EqualTo("eff0114.tga"));
            Assert.That(strip.SourceBlend, Is.EqualTo(5));
            Assert.That(strip.DestinationBlend, Is.EqualTo(2));
        }

        [Test]
        public void Parses_emitter_fixture()
        {
            // 01000011.par: version 13, 7 emitters, no strip/model arrays. Exercises the
            // per-emitter block including the v9+ emission-path (CEffPath) sub-block.
            // emitter[2] is a Fire emitter (PARTTICLE_FIRE=2) with a 3-frame size/angle/color ramp.
            using var stream = File.OpenRead(Fixtures.Path("par/01000011.par"));
            var file = ParFile.Read(stream);

            Assert.That(file.Version, Is.EqualTo(13u));
            Assert.That(file.PartName, Is.EqualTo("01000011"));
            Assert.That(file.Emitters.Length, Is.EqualTo(7));
            Assert.That(file.Strips.Length, Is.EqualTo(0));
            Assert.That(file.Models.Length, Is.EqualTo(0));

            var fire = file.Emitters[2];
            Assert.That(fire.Type, Is.EqualTo(ParticleSystemType.Fire));
            Assert.That(fire.ParticleCount, Is.EqualTo(26));
            Assert.That(fire.TextureName, Is.EqualTo("eff0083"));
            Assert.That(fire.ModelName, Is.EqualTo("RectPlane"));
            Assert.That(fire.Billboard, Is.EqualTo(1));
            Assert.That(fire.SourceBlend, Is.EqualTo(3));
            Assert.That(fire.DestinationBlend, Is.EqualTo(2));
            Assert.That(fire.Life, Is.EqualTo(2f));

            // frame range: a 3-keyframe size/angle/color ramp shared across all three arrays.
            Assert.That(fire.FrameSize.Length, Is.EqualTo(3));
            Assert.That(fire.FrameAngle.Length, Is.EqualTo(3));
            Assert.That(fire.FrameColor.Length, Is.EqualTo(3));
            Assert.That(fire.FrameSize[0], Is.EqualTo(1f));
            Assert.That(fire.FrameSize[1], Is.EqualTo(0.45f));
            Assert.That(fire.FrameSize[2], Is.EqualTo(0.1f));

            // emitter[1] carries a v9+ emission path: 100 points, 99 direction/distance segments.
            var strip = file.Emitters[1];
            Assert.That(strip.Type, Is.EqualTo(ParticleSystemType.Strip));
            Assert.That(strip.UsePath, Is.EqualTo(1));
            Assert.That(strip.Path, Is.Not.Null);
            Assert.That(strip.Path.Velocity, Is.EqualTo(2.5f));
            Assert.That(strip.Path.PathPoints.Length, Is.EqualTo(100));
            Assert.That(strip.Path.Directions.Length, Is.EqualTo(99));
            Assert.That(strip.Path.Distances.Length, Is.EqualTo(99));

            // dir/dist are interleaved per segment on disk (CEffPath::LoadPath, MPModelEff.cpp:139-147),
            // not two contiguous blocks. CEffPath derives each from the point gap
            // (dir = normalize(p[n+1]-p[n]), dist = |p[n+1]-p[n]|; MPModelEff.cpp:104-108), so a correct
            // de-interleaved read yields unit directions and distances equal to the gaps. A non-interleaved
            // read would misalign these (e.g. odd-indexed directions would be distance slots) and fail here.
            for (var k = 0; k < strip.Path.Directions.Length; k++)
            {
                Assert.That(strip.Path.Directions[k].Length(), Is.EqualTo(1f).Within(0.001),
                    $"direction {k} is not unit-length — dir/dist interleaving is wrong");
                var gap = (strip.Path.PathPoints[k + 1] - strip.Path.PathPoints[k]).Length();
                Assert.That(strip.Path.Distances[k].Value, Is.EqualTo(gap).Within(0.01),
                    $"distance {k} does not equal the gap to the next point");
            }
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("par/01030010.par", ParFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        public void Round_trips_emitter_fixture()
        {
            GoldenAssert.RoundTrips("par/01000011.par", ParFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("par", "*.par",
                ParFile.Read, (s, f) => f.Write(s));
        }
    }
}
