using System.Collections.Generic;
using NUnit.Framework;
using Top.Gltf;

namespace Top.Gltf.Tests
{
    public class GltfBuilderTests
    {
        [Test]
        public void Unused_lists_stay_null_because_gltf_rejects_empty_arrays()
        {
            var doc = new GltfBuilder("empty").Build().Document;

            Assert.That(doc.Nodes, Is.Null);
            Assert.That(doc.Meshes, Is.Null);
            Assert.That(doc.Materials, Is.Null);
            Assert.That(doc.Skins, Is.Null);
            Assert.That(doc.Animations, Is.Null);
            Assert.That(doc.Images, Is.Null);
            Assert.That(doc.Textures, Is.Null);
            Assert.That(doc.Scenes, Has.Count.EqualTo(1), "the scene is always there");
        }

        [Test]
        public void Children_accumulate_whatever_order_they_arrive_in()
        {
            var b = new GltfBuilder("scene");
            var parent = b.AddNode("parent");
            var first = b.AddNode("first");
            var second = b.AddNode("second");

            parent.AddChild(first);
            parent.AddChild(second);

            Assert.That(b.Document.Nodes[parent.Index].Children,
                Is.EqualTo(new[] { first.Index, second.Index }));
        }

        [Test]
        public void Roots_are_the_nodes_that_asked_to_be_roots()
        {
            var b = new GltfBuilder("scene");
            var root = b.AddNode("root").AsRoot();

            root.AddChild(b.AddNode("child"));

            Assert.That(b.Document.Scenes[0].Nodes, Is.EqualTo(new[] { root.Index }));
        }

        [Test]
        public void A_repeated_texture_uri_yields_the_same_texture()
        {
            var b = new GltfBuilder("scene");
            var first = b.AddTexture("../Textures/stone.png");
            var again = b.AddTexture("../Textures/stone.png");
            var other = b.AddTexture("../Textures/wood.png");

            Assert.That(again.Index, Is.EqualTo(first.Index));
            Assert.That(other.Index, Is.Not.EqualTo(first.Index));
            Assert.That(b.Document.Images, Has.Count.EqualTo(2));
            Assert.That(b.Document.Textures, Has.Count.EqualTo(2));
        }

        [Test]
        public void An_animation_that_never_got_a_channel_is_dropped()
        {
            var b = new GltfBuilder("scene");
            var node = b.AddNode("node").AsRoot();
            var input = b.Buffer.AddScalars(new[] { 0f, 1f }, withMinMax: true);

            b.AddAnimation("empty");
            b.AddAnimation("real").AddChannel(input, input, node, "translation");

            var doc = b.Build().Document;

            Assert.That(doc.Animations, Has.Count.EqualTo(1));
            Assert.That(doc.Animations[0].Name, Is.EqualTo("real"));
        }

        [Test]
        public void Dropping_every_animation_leaves_no_array_at_all()
        {
            var b = new GltfBuilder("scene");

            b.AddAnimation("empty");

            Assert.That(b.Build().Document.Animations, Is.Null);
        }

        [Test]
        public void A_node_reports_whether_it_kept_a_matrix()
        {
            var b = new GltfBuilder("scene");

            Assert.That(b.AddNode("plain").HasMatrix, Is.False);
            Assert.That(b.AddNode("posed").WithMatrix(new float[16]).HasMatrix, Is.True);
        }

        [Test]
        public void Primitives_bind_their_mesh_material_and_accessors()
        {
            var b = new GltfBuilder("scene");
            var material = b.AddMaterial("mat").WithAlphaMask(0.25f);
            var indices = b.Buffer.AddIndices(new uint[] { 0, 1, 2 });
            var mesh = b.AddMesh("mesh")
                .AddPrimitive(new Dictionary<string, int> { ["POSITION"] = 0 }, indices, material);
            var node = b.AddNode("node").WithMesh(mesh).AsRoot();

            var doc = b.Build().Document;

            Assert.That(doc.Nodes[node.Index].Mesh, Is.EqualTo(mesh.Index));
            Assert.That(doc.Meshes[mesh.Index].Primitives[0].Material, Is.EqualTo(material.Index));
            Assert.That(doc.Meshes[mesh.Index].Primitives[0].Indices, Is.EqualTo(indices));
            Assert.That(doc.Materials[material.Index].AlphaMode, Is.EqualTo("MASK"));
            Assert.That(doc.Materials[material.Index].AlphaCutoff, Is.EqualTo(0.25f));
        }
    }
}
