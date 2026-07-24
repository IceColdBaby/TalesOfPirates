using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor.Tests
{
    public class GltfImporterHostingTests
    {
        private const string TempDir = "Assets/TempGltfHostingTests";
        private const string GlbPath = TempDir + "/hosting.glb";

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempDir);

            var doc = new GltfDocument
            {
                Scene = 0,
                Scenes = new List<GltfScene> { new GltfScene { Nodes = new List<int> { 0, 3 } } },
                Nodes = new List<GltfNode>
                {
                    new GltfNode { Name = "skel", Children = new List<int> { 1, 4 } },
                    new GltfNode { Name = "b0", Children = new List<int> { 2 } },
                    new GltfNode { Name = "b1" },
                    new GltfNode { Name = "other" },
                    new GltfNode { Name = "c0" },
                },
                Meshes = null,
            };
            var builder = new BufferBuilder(doc);
            var input = builder.AddScalars(new[] { 0f, 1f }, withMinMax: true);
            var output = builder.AddVec4(new[]
            {
                new System.Numerics.Vector4(0, 0, 0, 1),
                new System.Numerics.Vector4(0, 0.7071f, 0, 0.7071f),
            });

            doc.Animations = new List<GltfAnimation>
            {
                Clip("subtree", input, output, 1, 2),
                Clip("spread", input, output, 1, 3),
                Clip("siblings", input, output, 2, 4),
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
        public void Subtree_clip_hosts_on_its_root_target()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var b0 = root.transform.Find("skel/b0");

            Assert.That(b0.GetComponent<Animation>(), Is.Not.Null);
            Assert.That(b0.GetComponent<Animation>().clip.name, Is.EqualTo("subtree"));
            Assert.That(root.transform.Find("skel").GetComponent<Animation>().GetClip("subtree"),
                Is.Null);
        }

        [Test]
        public void Subtree_clip_paths_are_relative_to_the_host()
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
                .OfType<AnimationClip>().Single(c => c.name == "subtree");

            var paths = AnimationUtility.GetCurveBindings(clip)
                .Select(b => b.path).Distinct().OrderBy(p => p).ToArray();

            Assert.That(paths, Is.EqualTo(new[] { string.Empty, "b1" }));
        }

        [Test]
        public void Clip_without_a_common_ancestor_hosts_on_the_model_root()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var clip = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
                .OfType<AnimationClip>().Single(c => c.name == "spread");

            var paths = AnimationUtility.GetCurveBindings(clip)
                .Select(b => b.path).Distinct().OrderBy(p => p).ToArray();

            Assert.That(root.GetComponent<Animation>(), Is.Not.Null);
            Assert.That(paths, Is.EqualTo(new[] { "other", "skel/b0" }));
        }

        [Test]
        public void Sibling_targets_host_on_their_shared_parent()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var skel = root.transform.Find("skel");
            var clip = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
                .OfType<AnimationClip>().Single(c => c.name == "siblings");

            var paths = AnimationUtility.GetCurveBindings(clip)
                .Select(b => b.path).Distinct().OrderBy(p => p).ToArray();

            Assert.That(skel.GetComponent<Animation>(), Is.Not.Null);
            Assert.That(skel.GetComponent<Animation>().clip.name, Is.EqualTo("siblings"));
            Assert.That(paths, Is.EqualTo(new[] { "b0/b1", "c0" }));
        }
    }
}
