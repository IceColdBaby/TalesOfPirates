using System.IO;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Tests
{
    public class ModelTests
    {
        private static SceneModel Load()
        {
            using var stream = File.OpenRead(Fixtures.Path("lmo/by-bd015.lmo"));
            return LmoFile.Read(stream).Model;
        }

        [Test]
        public void Maps_all_objects_and_helper_meshes_to_nodes()
        {
            var model = Load();
            var file = GltfExport.Model("by-bd015", model);
            var doc = file.Document;

            Assert.That(doc.Nodes.Count, Is.EqualTo(23));
            Assert.That(doc.Nodes.Count(n => n.Name.StartsWith("geom_")), Is.EqualTo(22));
            Assert.That(doc.Nodes.Count(n => n.Name == "helper_block_0"), Is.EqualTo(1));
            Assert.That(doc.Meshes.Count, Is.EqualTo(23));
            Assert.That(doc.Scenes[0].Nodes.Count, Is.EqualTo(23), "all by-bd015 nodes are roots");
        }

        [Test]
        public void First_object_keeps_subset_order_as_primitive_order()
        {
            var model = Load();
            var doc = GltfExport.Model("by-bd015", model).Document;
            var mesh = doc.Meshes.First(m => m.Name == "geom_0");

            Assert.That(mesh.Primitives.Count, Is.EqualTo(2));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("POSITION"));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("NORMAL"));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("COLOR_0"));
            Assert.That(mesh.Primitives[0].Attributes, Contains.Key("TEXCOORD_0"));
        }

        [Test]
        public void Positions_swap_up_axis()
        {
            var model = Load();
            var file = GltfExport.Model("by-bd015", model);
            var doc = file.Document;
            var data = new GltfData(
                new GltfFile { Document = doc, BinChunk = file.BinChunk });

            var obj = model.GeometryObjects[0];
            var accessor = doc.Meshes
                .First(m => m.Name == "geom_0").Primitives[0].Attributes["POSITION"];
            var floats = data.ReadFloats(accessor);

            Assert.That(floats[0], Is.EqualTo(obj.Mesh.Vertices[0].X));
            Assert.That(floats[1], Is.EqualTo(obj.Mesh.Vertices[0].Z));
            Assert.That(floats[2], Is.EqualTo(obj.Mesh.Vertices[0].Y));
        }

        [Test]
        public void Triangle_winding_is_reversed()
        {
            var model = Load();
            var file = GltfExport.Model("by-bd015", model);
            var doc = file.Document;
            var data = new GltfData(
                new GltfFile { Document = doc, BinChunk = file.BinChunk });

            var obj = model.GeometryObjects[0];
            var subset = obj.Mesh.Subsets[0];
            var primitive = doc.Meshes.First(m => m.Name == "geom_0").Primitives[0];
            var indices = data.ReadInts(primitive.Indices.Value);

            Assert.That(indices[0], Is.EqualTo((int)obj.Mesh.Indices[subset.StartIndex]));
            Assert.That(indices[1], Is.EqualTo((int)obj.Mesh.Indices[subset.StartIndex + 2]));
            Assert.That(indices[2], Is.EqualTo((int)obj.Mesh.Indices[subset.StartIndex + 1]));
        }

        [Test]
        public void Node_matrix_swaps_up_axis()
        {
            var model = Load();
            var doc = GltfExport.Model("by-bd015", model).Document;
            var obj = model.GeometryObjects[0];
            var node = doc.Nodes.First(n => n.Name == "geom_0");

            Assert.That(node.Matrix[0], Is.EqualTo(obj.LocalMatrix.M11));
            Assert.That(node.Matrix[2], Is.EqualTo(obj.LocalMatrix.M12));
            Assert.That(node.Matrix[13], Is.EqualTo(obj.LocalMatrix.M43),
                "up translation lands in glTF y");
            Assert.That(node.Matrix[14], Is.EqualTo(obj.LocalMatrix.M42));
        }

        [Test]
        public void Child_objects_nest_under_the_node_their_parent_index_names()
        {
            var model = new SceneModel
            {
                GeometryObjects =
                [
                    Fixtures.MakeObject(id: 5, parentId: uint.MaxValue),
                    Fixtures.MakeObject(id: 9, parentId: 0)
                ],
                Helpers = [],
            };

            var doc = GltfExport.Model("synthetic", model).Document;

            Assert.That(doc.Scenes[0].Nodes, Is.EqualTo(new[] { 0 }),
                "only the parent is a scene root");
            Assert.That(doc.Nodes[0].Name, Is.EqualTo("geom_5"));
            Assert.That(doc.Nodes[0].Children, Is.EqualTo(new[] { 1 }));
            Assert.That(doc.Nodes[1].Name, Is.EqualTo("geom_9"));
        }

        [Test]
        public void A_parent_index_past_the_sequence_leaves_the_object_a_root()
        {
            var model = new SceneModel
            {
                GeometryObjects =
                [
                    Fixtures.MakeObject(id: 5, parentId: uint.MaxValue),
                    Fixtures.MakeObject(id: 9, parentId: 5)
                ],
                Helpers = [],
            };

            var doc = GltfExport.Model("synthetic", model).Document;

            Assert.That(doc.Scenes[0].Nodes, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void Matrix_animation_stays_on_the_object_that_carries_it_when_ids_repeat()
        {
            var model = new SceneModel
            {
                GeometryObjects =
                [
                    Fixtures.MakeObject(id: 5, parentId: uint.MaxValue),
                    Fixtures.MakeObject(id: 5, parentId: uint.MaxValue)
                ],
                Helpers = [],
            };

            model.GeometryObjects[0].Animation = new AnimationData
            {
                Matrix = new MatrixAnimation
                {
                    Frames = [Matrix4x4.Identity, Matrix4x4.CreateTranslation(1f, 0f, 0f)],
                },
            };

            var doc = GltfExport.Model("twins", model).Document;

            Assert.That(doc.Animations, Has.Count.EqualTo(1));
            Assert.That(doc.Animations[0].Channels.Select(c => c.Target.Node.Value).Distinct(),
                Is.EqualTo(new[] { 0 }), "the first twin carries the animation");
        }

        [Test]
        public void Matrix_animations_become_per_node_gltf_animations()
        {
            var model = Load();
            var file = GltfExport.Model("by-bd015", model);
            var doc = file.Document;

            Assert.That(doc.Animations, Has.Count.EqualTo(11));

            var geom3 = doc.Nodes.FindIndex(n => n.Name == "geom_3");
            var animation = doc.Animations.Find(a => a.Name == "geom_3");
            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.Channels, Has.Count.EqualTo(3));
            Assert.That(animation.Channels.TrueForAll(c => c.Target.Node == geom3));

            var rotationChannel = animation.Channels.Find(c => c.Target.Path == "rotation");
            var sampler = animation.Samplers[rotationChannel.Sampler];
            Assert.That(doc.Accessors[sampler.Input].Count, Is.EqualTo(701));
            Assert.That(doc.Accessors[sampler.Input].Max[0],
                Is.EqualTo(700f / 30f).Within(0.001f));

            var pendulum = doc.Animations.Find(a => a.Name == "geom_13");
            var pendulumInput = pendulum.Samplers[pendulum.Channels[0].Sampler].Input;
            Assert.That(doc.Accessors[pendulumInput].Max[0],
                Is.EqualTo(60f / 30f).Within(0.001f), "pendulum loops at its own 2s cycle");
        }

        [Test]
        public void Animated_nodes_use_trs_instead_of_matrix()
        {
            var model = Load();
            var doc = GltfExport.Model("by-bd015", model).Document;
            var node = doc.Nodes.First(n => n.Name == "geom_3");

            Assert.That(node.Matrix, Is.Null);
            Assert.That(node.Translation, Is.Not.Null);
            Assert.That(node.Rotation, Is.Not.Null);
            Assert.That(node.Scale, Is.Not.Null);
        }

        [Test]
        public void Preview_materials_reference_shared_textures()
        {
            var model = Load();
            var doc = GltfExport.Model("by-bd015", model, "../../Textures").Document;

            Assert.That(doc.Materials, Is.Not.Null);
            Assert.That(doc.Images.TrueForAll(i => i.Uri.StartsWith("../../Textures/") && i.Uri.EndsWith(".png")));

            var mesh = doc.Meshes.Find(m => m.Name == "geom_0");
            Assert.That(mesh.Primitives.TrueForAll(p => p.Material != null));
            Assert.That(doc.Materials[mesh.Primitives[0].Material.Value].Name,
                Is.EqualTo("by-bd015_0_0"));

            Assert.That(doc.Materials.Select(m => m.Name), Is.Unique);
            Assert.That(doc.Materials.Exists(m => m.AlphaMode == "MASK"));
            Assert.That(doc.Materials.Exists(m => m.AlphaMode == "BLEND"));
        }

        [Test]
        public void Geometry_only_output_has_no_materials()
        {
            var model = Load();
            var doc = GltfExport.Model("by-bd015", model).Document;

            Assert.That(doc.Materials, Is.Null);
        }
    }
}
