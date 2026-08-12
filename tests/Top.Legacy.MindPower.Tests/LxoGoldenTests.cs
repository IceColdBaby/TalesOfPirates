using System.IO;
using NUnit.Framework;
using Top.Legacy.MindPower.Geometry;

namespace Top.Legacy.MindPower.Tests
{
    public class LxoGoldenTests
    {
        [Test]
        public void Parses_known_fixture()
        {
            using var stream = File.OpenRead(Fixtures.Path("lxo/login03.lxo"));
            var tree = LxoFile.Read(stream).Tree;

            Assert.That(tree.Version, Is.EqualTo(0x1005u));
            Assert.That(tree.Nodes.Length, Is.EqualTo(59));

            // Root is a DUMMY node with an invalid (root) handle and parent.
            var root = tree.Nodes[0];
            Assert.That(root.Type, Is.EqualTo(SceneNodeType.Dummy));
            Assert.That(root.Handle, Is.EqualTo(0u));
            Assert.That(root.ParentHandle, Is.EqualTo(0xFFFFFFFFu));

            // First Primitive node (handle 24, id 1) carries a 731-vertex mesh.
            var primitive = tree.Nodes[2];
            Assert.That(primitive.Type, Is.EqualTo(SceneNodeType.Primitive));
            var geometry = (GeometryObject)primitive.Data;
            Assert.That(geometry.Mesh.Vertices.Length, Is.EqualTo(731));
            Assert.That(geometry.Mesh.Vertices[0],
                Is.EqualTo(new System.Numerics.Vector3(3.254573f, 25.371635f, 1.7700318f)));
        }

        [Test]
        public void Round_trips_fixture()
        {
            GoldenAssert.RoundTrips("lxo/login03.lxo", LxoFile.Read, (s, f) => f.Write(s));
        }

        [Test]
        [Category("Fidelity")]
        public void Round_trips_all_fixtures()
        {
            GoldenAssert.RoundTripsAll("lxo", "*.lxo",
                LxoFile.Read, (s, f) => f.Write(s));
        }
    }
}
