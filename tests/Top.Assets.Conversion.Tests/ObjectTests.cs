using System.Numerics;
using NUnit.Framework;
using Top.Assets.Conversion.Models.Gltf;
using Top.Gltf;
using Top.MindPower;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
{
    public class ObjectTests
    {
        [Test]
        public void Single_object_maps_to_one_root_node()
        {
            var doc = GltfExport.Object("single", Fixtures.MakeObject(id: 5, parentId: uint.MaxValue))
                .Document;

            Assert.That(doc.Nodes, Has.Count.EqualTo(1));
            Assert.That(doc.Nodes[0].Name, Is.EqualTo("geom_5"));
            Assert.That(doc.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Helper_dummies_become_locator_nodes()
        {
            var obj = Fixtures.MakeObject(id: 1, parentId: uint.MaxValue);
            obj.Helper = new Helper
            {
                Dummies =
                [
                    new HelperDummy
                    {
                        Id = 4,
                        Matrix = Matrix4x4.CreateTranslation(1f, 2f, 3f),
                    }
                ],
            };

            var doc = GltfExport.Object("dummies", obj).Document;
            var node = doc.Nodes.Find(n => n.Name == "dummy_4");

            Assert.That(node, Is.Not.Null);
            Assert.That(node.Mesh, Is.Null);
            Assert.That(doc.Scenes[0].Nodes, Has.Member(doc.Nodes.IndexOf(node)));
            Assert.That(node.Matrix[12], Is.EqualTo(1f));
            Assert.That(node.Matrix[13], Is.EqualTo(3f), "up translation lands in glTF y");
            Assert.That(node.Matrix[14], Is.EqualTo(2f));
        }

        [Test]
        public void Helper_meshes_carry_their_source_name()
        {
            var obj = Fixtures.MakeObject(id: 1, parentId: uint.MaxValue);
            obj.Helper = new Helper
            {
                Meshes =
                [
                    MakeHelperMesh("Block"),
                    MakeHelperMesh(null)
                ],
            };

            var doc = GltfExport.Object("helpers", obj).Document;

            Assert.That(doc.Nodes.Exists(n => n.Name == "helper_block_0"), Is.True);
            Assert.That(doc.Nodes.Exists(n => n.Name == "helper_mesh_1"), Is.True);
        }

        [Test]
        public void Degenerate_normals_are_replaced()
        {
            var obj = Fixtures.MakeObject(id: 8, parentId: uint.MaxValue);
            obj.Mesh.Normals =
            [
                new Vector3(float.NaN, 0, 0),
                Vector3.Zero,
                new Vector3(2, 0, 0)
            ];

            var file = GltfExport.Object("normals", obj);
            var doc = file.Document;
            var data = new GltfData(
                new GltfFile { Document = doc, BinChunk = file.BinChunk });
            var accessor = doc.Meshes[0].Primitives[0].Attributes["NORMAL"];
            var floats = data.ReadFloats(accessor);

            // NaN and zero-length normals fall back to source-space up, which
            // the axis swap turns into glTF (0, 1, 0); valid ones renormalize.
            Assert.That(floats, Is.EqualTo(new[]
            {
                0f, 1f, 0f,
                0f, 1f, 0f,
                1f, 0f, 0f,
            }));
        }

        [Test]
        public void Lit_subset_splits_into_child_node()
        {
            var obj = Fixtures.MakeObject(id: 7, parentId: uint.MaxValue);
            obj.Mesh.Indices = [0, 1, 2, 0, 1, 2];
            obj.Mesh.Subsets =
            [
                new MeshSubset { PrimitiveCount = 1, StartIndex = 0 },
                new MeshSubset { PrimitiveCount = 1, StartIndex = 3 }
            ];

            var doc = GltfExport.Object("weapon", obj, null, 1).Document;

            Assert.That(doc.Nodes, Has.Count.EqualTo(2));
            Assert.That(doc.Nodes[0].Name, Is.EqualTo("geom_7"));
            Assert.That(doc.Nodes[0].Children, Is.EqualTo(new[] { 1 }));
            Assert.That(doc.Nodes[1].Name, Is.EqualTo("geom_7_lit"));
            Assert.That(doc.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }), "lit node is not a scene root");
            Assert.That(doc.Meshes[0].Primitives, Has.Count.EqualTo(1));
            Assert.That(doc.Meshes[1].Primitives, Has.Count.EqualTo(1));
        }

        [Test]
        public void Lit_subset_needs_a_second_subset()
        {
            var doc = GltfExport
                .Object("single", Fixtures.MakeObject(id: 3, parentId: uint.MaxValue), null, litSubset: 0)
                .Document;

            Assert.That(doc.Nodes, Has.Count.EqualTo(1));
            Assert.That(doc.Meshes, Has.Count.EqualTo(1));
            Assert.That(doc.Meshes[0].Primitives, Has.Count.EqualTo(1));
        }

        [Test]
        public void Static_opacity_emits_blend_and_a_base_color_factor()
        {
            var obj = Fixtures.MakeObject(0, uint.MaxValue);
            obj.Materials =
            [
                new MaterialTexture { Transparency = TransparencyType.Filter, Opacity = 0.3f }
            ];

            var material = GltfExport.Object("translucent", obj, "../Textures")
                .Document.Materials[0];

            Assert.That(material.AlphaMode, Is.EqualTo("BLEND"));
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor, Has.Length.EqualTo(4));
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor[3],
                Is.EqualTo(0.3f).Within(1e-6f));
        }

        [Test]
        public void Opaque_materials_carry_no_base_color_factor()
        {
            var obj = Fixtures.MakeObject(0, uint.MaxValue);
            obj.Materials = [new MaterialTexture { Transparency = TransparencyType.Filter }];

            var material = GltfExport.Object("opaque", obj, "../Textures").Document.Materials[0];

            Assert.That(material.AlphaMode, Is.Null);
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor, Is.Null);
        }

        [Test]
        public void An_opacity_track_emits_blend_at_full_opacity()
        {
            var obj = Fixtures.MakeObject(0, uint.MaxValue);
            obj.Materials =
            [
                new MaterialTexture { Transparency = TransparencyType.Filter, Opacity = 1f }
            ];
            obj.Animation = new AnimationData
            {
                MaterialOpacity =
                [
                    new MaterialOpacityAnimation { Keys = new FloatKeyframe[2] }
                ],
            };

            var material = GltfExport.Object("animated", obj, "../Textures").Document.Materials[0];

            Assert.That(material.AlphaMode, Is.EqualTo("BLEND"));
            Assert.That(material.PbrMetallicRoughness.BaseColorFactor, Is.Null,
                "the stored opacity is still one; only the track moves it");
        }

        [Test]
        public void Alpha_blend_atom_on_a_filter_material_emits_blend()
        {
            var obj = Fixtures.MakeObject(0, uint.MaxValue);
            obj.Materials =
            [
                new MaterialTexture
                {
                    Transparency = TransparencyType.Filter,
                    RenderStates = [new RenderStateAtom { State = 27, Value0 = 1 }],
                }
            ];

            var material = GltfExport.Object("blended", obj, "../Textures").Document.Materials[0];

            Assert.That(material.AlphaMode, Is.EqualTo("BLEND"));
        }

        private static HelperMesh MakeHelperMesh(string name)
        {
            return new HelperMesh
            {
                Name = name,
                Matrix = Matrix4x4.Identity,
                Vertices =
                [
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0)
                ],
                Faces =
                [
                    new HelperMeshFace { Vertex = [0, 1, 2] }
                ],
            };
        }
    }
}
