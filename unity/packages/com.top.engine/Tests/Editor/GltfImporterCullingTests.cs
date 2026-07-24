using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor.Tests
{
    /// <summary>
    /// A bone clip's host can be a renderer-less node (a skeleton root with
    /// only bone children; the SkinnedMeshRenderer lives on a sibling node),
    /// and AnimationCullingType.BasedOnRenderers never samples a clip with
    /// no renderer under its own hierarchy. The importer must fall back to
    /// AlwaysAnimate for such hosts while renderer-bearing hosts keep
    /// BasedOnRenderers.
    /// </summary>
    public class GltfImporterCullingTests
    {
        private const string TempDir = "Assets/TempGltfCullingTests";
        private const string GlbPath = TempDir + "/culling.glb";

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempDir);

            var doc = new GltfDocument
            {
                Scene = 0,
                Scenes = new List<GltfScene> { new GltfScene { Nodes = new List<int> { 0, 1, 3 } } },
                Nodes = new List<GltfNode>
                {
                    new GltfNode { Name = "mesh_sibling", Mesh = 0 },
                    new GltfNode { Name = "skel", Children = new List<int> { 2 } },
                    new GltfNode { Name = "bone1" },
                    new GltfNode { Name = "rendered_parent", Mesh = 0, Children = new List<int> { 4 } },
                    new GltfNode { Name = "rendered_child" },
                },
                Meshes = new List<GltfMesh> { new GltfMesh { Name = "mesh" } },
                Materials = new List<GltfMaterial> { new GltfMaterial { Name = "mat" } },
            };
            var builder = new BufferBuilder(doc);
            var positions = builder.AddVec3(new[]
            {
                new System.Numerics.Vector3(1, 2, 3),
                new System.Numerics.Vector3(4, 5, 6),
                new System.Numerics.Vector3(7, 8, 9),
            }, withMinMax: true);
            doc.Meshes[0].Primitives.Add(new GltfPrimitive
            {
                Attributes = { ["POSITION"] = positions },
                Indices = builder.AddIndices(new uint[] { 0, 1, 2 }),
                Material = 0,
            });

            var input = builder.AddScalars(new[] { 0f, 1f }, withMinMax: true);
            var output = builder.AddVec4(new[]
            {
                new System.Numerics.Vector4(0, 0, 0, 1),
                new System.Numerics.Vector4(0, 0.7071f, 0, 0.7071f),
            });

            doc.Animations = new List<GltfAnimation>
            {
                Clip("bone_clip", input, output, 1, 2),
                Clip("rendered_clip", input, output, 3, 4),
            };

            var bin = builder.Finish();
            using (var fs = File.Create(GlbPath))
            {
                GltfWriter.WriteGlb(doc, bin, fs);
            }

            AssetDatabase.ImportAsset(GlbPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static GltfAnimation Clip(string name, int input, int output, int nodeA, int nodeB)
        {
            var animation = new GltfAnimation { Name = name };

            foreach (var node in new[] { nodeA, nodeB })
            {
                animation.Samplers.Add(new GltfAnimationSampler
                {
                    Input = input, Output = output, Interpolation = "LINEAR",
                });
                animation.Channels.Add(new GltfAnimationChannel
                {
                    Sampler = animation.Samplers.Count - 1,
                    Target = { Node = node, Path = "rotation" },
                });
            }

            return animation;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempDir);
        }

        [Test]
        public void Renderer_less_host_gets_always_animate()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var skel = root.transform.Find("skel");
            var animation = skel.GetComponent<Animation>();

            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.clip.name, Is.EqualTo("bone_clip"));
            Assert.That(skel.GetComponentInChildren<Renderer>(true), Is.Null,
                "the sibling mesh node must not count as being under the skeleton host");
            Assert.That(animation.cullingType, Is.EqualTo(AnimationCullingType.AlwaysAnimate));
        }

        [Test]
        public void Host_with_a_renderer_in_its_own_hierarchy_keeps_based_on_renderers()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var renderedParent = root.transform.Find("rendered_parent");
            var animation = renderedParent.GetComponent<Animation>();

            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.clip.name, Is.EqualTo("rendered_clip"));
            Assert.That(animation.cullingType, Is.EqualTo(AnimationCullingType.BasedOnRenderers));
        }
    }
}
