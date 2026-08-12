using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Top.Conversion.Gltf.Tests
{
    public class SchemaTests
    {
        [Test]
        public void Serializes_minimal_document()
        {
            var doc = new GltfDocument();
            var json = GltfJson.Serialize(doc, indented: false);

            Assert.That(json, Is.EqualTo("{\"asset\":{\"version\":\"2.0\"}}"));
        }

        [Test]
        public void Round_trips_extras_on_nodes()
        {
            var doc = new GltfDocument
            {
                Nodes =
                [
                    new GltfNode { Name = "n", Extras = JObject.Parse("{\"TOP_test\":42}") }
                ],
            };

            var back = GltfJson.Deserialize(GltfJson.Serialize(doc, indented: false));

            Assert.That((int)back.Nodes[0].Extras["TOP_test"], Is.EqualTo(42));
        }

        [Test]
        public void Preserves_unknown_root_members()
        {
            const string json = "{\"asset\":{\"version\":\"2.0\"},\"cameras\":[{\"name\":\"c\"}]}";

            var doc = GltfJson.Deserialize(json);
            var again = GltfJson.Serialize(doc, indented: false);

            Assert.That(doc.Rest, Contains.Key("cameras"));
            Assert.That(again, Does.Contain("\"cameras\""));
        }

        [Test]
        public void Round_trips_skins()
        {
            var doc = new GltfDocument
            {
                Nodes =
                [
                    new GltfNode { Name = "joint" },
                    new GltfNode { Name = "skinned", Mesh = 0, Skin = 0 }
                ],
                Skins =
                [
                    new GltfSkin
                    {
                        Name = "rig",
                        InverseBindMatrices = 4,
                        Skeleton = 0,
                        Joints = [0],
                    }
                ],
            };

            var back = GltfJson.Deserialize(GltfJson.Serialize(doc, indented: false));

            Assert.That(back.Skins[0].Name, Is.EqualTo("rig"));
            Assert.That(back.Skins[0].InverseBindMatrices, Is.EqualTo(4));
            Assert.That(back.Skins[0].Skeleton, Is.EqualTo(0));
            Assert.That(back.Skins[0].Joints, Is.EqualTo(new[] { 0 }));
            Assert.That(back.Nodes[1].Skin, Is.EqualTo(0));
        }

        [Test]
        public void Omits_default_normalized_flag()
        {
            var doc = new GltfDocument
            {
                Accessors =
                [
                    new GltfAccessor { ComponentType = GltfConst.Float, Count = 1, Type = "SCALAR" }
                ],
            };

            Assert.That(GltfJson.Serialize(doc, indented: false), Does.Not.Contain("normalized"));
        }
    }
}
