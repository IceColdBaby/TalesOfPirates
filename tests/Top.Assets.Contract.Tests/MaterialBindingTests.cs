using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Top.Assets.Contract.Models.Materials;
using Top.Gltf;

namespace Top.Assets.Contract.Tests
{
    public class MaterialBindingTests
    {
        private static GltfMeshRef Primitive(GltfBuilder builder, GltfMeshRef mesh, GltfMaterialRef material)
        {
            return mesh.AddPrimitive(
                new Dictionary<string, int>
                {
                    ["POSITION"] = builder.Buffer.AddVec3(new[] { Vector3.Zero }, withMinMax: true),
                },
                builder.Buffer.AddIndices(new uint[] { 0, 0, 0 }),
                material);
        }

        [Test]
        public void A_material_binds_to_the_node_and_slot_that_render_it()
        {
            var builder = new GltfBuilder("scene");
            var mesh = builder.AddMesh("geom_0");

            Primitive(builder, mesh, builder.AddMaterial("m_0_0"));
            Primitive(builder, mesh, builder.AddMaterial("m_0_1"));

            builder.AddNode("geom_0").WithMesh(mesh).AsRoot();

            var bindings = MaterialBinding.In(builder.Document);

            Assert.That(bindings[1].Single().NodeName, Is.EqualTo("geom_0"));
            Assert.That(bindings[1].Single().SubMesh, Is.EqualTo(1));
        }

        [Test]
        public void A_material_split_onto_a_lit_shell_binds_to_the_shell_node()
        {
            var builder = new GltfBuilder("scene");
            var mesh = builder.AddMesh("geom_0");
            var shell = builder.AddMesh("geom_0_lit");

            Primitive(builder, mesh, builder.AddMaterial("m_0_0"));
            Primitive(builder, shell, builder.AddMaterial("m_0_1"));
            Primitive(builder, mesh, builder.AddMaterial("m_0_2"));

            builder
                .AddNode("geom_0")
                .WithMesh(mesh)
                .AddChild(builder.AddNode("geom_0_lit").WithMesh(shell))
                .AsRoot();

            var bindings = MaterialBinding.In(builder.Document);

            Assert.That(bindings[1].Single().NodeName, Is.EqualTo("geom_0_lit"));
            Assert.That(bindings[1].Single().SubMesh, Is.Zero);
            Assert.That(bindings[2].Single().SubMesh, Is.EqualTo(1),
                "the shell's subset is gone from the main mesh, so the slots close up");
        }

        [Test]
        public void A_mesh_reused_by_two_nodes_binds_to_both()
        {
            var builder = new GltfBuilder("scene");
            var mesh = builder.AddMesh("geom_0");

            Primitive(builder, mesh, builder.AddMaterial("m_0_0"));

            builder.AddNode("geom_0").WithMesh(mesh).AsRoot();
            builder.AddNode("geom_1").WithMesh(mesh).AsRoot();

            Assert.That(MaterialBinding.In(builder.Document)[0].Select(binding => binding.NodeName),
                Is.EqualTo(new[] { "geom_0", "geom_1" }));
        }

        [Test]
        public void A_material_nothing_renders_binds_to_nothing()
        {
            var builder = new GltfBuilder("scene");

            builder.AddMaterial("unused");

            Assert.That(MaterialBinding.In(builder.Document)[0], Is.Empty);
        }

        [Test]
        public void A_document_without_nodes_binds_nothing()
        {
            Assert.That(MaterialBinding.In(new GltfDocument())[0], Is.Empty);
        }
    }
}
