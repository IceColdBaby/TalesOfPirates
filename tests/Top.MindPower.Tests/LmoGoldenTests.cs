using System.IO;
using NUnit.Framework;
using Top.MindPower;
using Top.MindPower.Geometry;

namespace Top.MindPower.Tests
{
    public class LmoGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            using var stream = File.OpenRead(Fixtures.Path("lmo/yyyy080.lmo"));
            var model = LmoFile.Read(stream).Model;

            Assert.That(model.Version, Is.EqualTo(0u));
            Assert.That(model.GeometryObjects.Length, Is.EqualTo(1));
            Assert.That(model.Helpers.Length, Is.EqualTo(0));

            var obj = model.GeometryObjects[0];
            Assert.That(obj.Id, Is.EqualTo(0u));
            Assert.That(obj.ParentId, Is.EqualTo(0xFFFFFFFFu));

            Assert.That(obj.Mesh, Is.Not.Null);
            Assert.That(obj.Materials.Length, Is.EqualTo(1));

            Assert.That(obj.Helper, Is.Not.Null);
            Assert.That(obj.Helper.Type, Is.EqualTo(HelperType.BoundingBox));
            Assert.That(obj.Helper.BoundingBoxes.Length, Is.EqualTo(1));
            Assert.That(obj.Helper.BoundingBoxes[0].Id, Is.EqualTo(0u));

            // The bounding box's translation is the pose-object seat (≈ y=1.024).
            Assert.That(obj.Helper.BoundingBoxes[0].Matrix.M42, Is.EqualTo(1.0236406f));
        }

        [Test]
        public void Rejects_stale_header_files_like_the_engine()
        {
            using var stream = File.OpenRead(Fixtures.Path("rejected/nml-bd016.lmo"));

            var e = Assert.Throws<ParseException>(() => LmoFile.Read(stream));

            Assert.That(e.Message, Does.Contain("implausible material block size"));
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("lmo/yyyy080.lmo", LmoFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("lmo", "*.lmo",
                LmoFile.Read, (s, f) => f.Write(s));
        }
    }
}
