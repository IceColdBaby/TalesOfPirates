using System.Linq;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Gltf.Tests
{
    public class GltfObjectBuilderTests
    {
        private Material _material;
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            var builder = new GltfBuilder("tri");
            var mesh = TestGltf.Triangle(builder, "tri_mesh", "tri_mat");
            var node = builder.AddNode("tri").WithMesh(mesh).AsRoot();
            var turn = TestGltf.QuarterTurn(builder);
            builder.AddAnimation("default")
                .AddChannel(turn.Input, turn.Output, node, "rotation");

            _material = new Material(Shader.Find("Top/Legacy"));
            _root = GltfObjectBuilder.Build(new GltfData(builder.Build()), "tri",
                new[] { _material }, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_material);
        }

        private Mesh Mesh()
        {
            return _root.GetComponentInChildren<MeshFilter>(true).sharedMesh;
        }

        [Test]
        public void Builds_one_mesh_per_gltf_mesh_keeping_its_name()
        {
            Assert.That(Mesh().name, Is.EqualTo("tri_mesh"));
            Assert.That(Mesh().vertexCount, Is.EqualTo(3));
        }

        [Test]
        public void Positions_are_z_negated()
        {
            Assert.That(Mesh().vertices[0], Is.EqualTo(new Vector3(1, 2, -3)));
        }

        [Test]
        public void Winding_is_reversed()
        {
            Assert.That(Mesh().triangles, Is.EqualTo(new[] { 0, 2, 1 }));
        }

        [Test]
        public void Root_parents_the_nodes_that_have_no_parent()
        {
            Assert.That(_root.transform.childCount, Is.EqualTo(1));
            Assert.That(_root.transform.GetChild(0).name, Is.EqualTo("tri"));
        }

        [Test]
        public void Renderer_binds_the_material_the_primitive_indexes()
        {
            var renderer = _root.GetComponentInChildren<MeshRenderer>();

            Assert.That(renderer.sharedMaterials, Is.EqualTo(new[] { _material }));
        }

        [Test]
        public void Animations_become_looping_legacy_clips()
        {
            var clip = _root.GetComponentInChildren<Animation>().clip;

            Assert.That(clip.legacy, Is.True);
            Assert.That(clip.wrapMode, Is.EqualTo(WrapMode.Loop));
            Assert.That(clip.name, Is.EqualTo("default"));
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.That(bindings.Length, Is.EqualTo(4));
            Assert.That(bindings.Select(b => b.path), Is.All.EqualTo(string.Empty));
        }

        [Test]
        public void Animation_component_lands_on_the_target_node()
        {
            var animation = _root.transform.Find("tri").GetComponent<Animation>();

            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.clip.name, Is.EqualTo("default"));
            Assert.That(animation.playAutomatically, Is.True);
            Assert.That(animation.cullingType, Is.EqualTo(AnimationCullingType.BasedOnRenderers));
        }
    }
}
