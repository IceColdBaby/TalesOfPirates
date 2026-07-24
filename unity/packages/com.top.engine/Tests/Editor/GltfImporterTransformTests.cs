using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor.Tests
{
    public class GltfImporterTransformTests
    {
        private const string TempDir = "Assets/TempGltfTransformTests";
        private const string GlbPath = TempDir + "/shear.glb";

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempDir);

            var doc = new GltfDocument
            {
                Scene = 0,
                Scenes = new List<GltfScene> { new GltfScene { Nodes = new List<int> { 0 } } },
                Nodes = new List<GltfNode>
                {
                    new GltfNode
                    {
                        Name = "sheared",
                        Matrix = new[]
                        {
                            1f, 0f, 0f, 0f,
                            0.5f, 1f, 0f, 0f,
                            0f, 0f, 1f, 0f,
                            1f, 2f, 0f, 1f,
                        },
                    },
                },
            };
            var builder = new BufferBuilder(doc);
            builder.AddScalars(new[] { 0f }, withMinMax: true);
            var bin = builder.Finish();

            using (var fs = File.Create(GlbPath))
            {
                GltfWriter.WriteGlb(doc, bin, fs);
            }

            AssetDatabase.ImportAsset(GlbPath, ImportAssetOptions.ForceSynchronousImport);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempDir);
        }

        [Test]
        public void Sheared_matrix_nodes_decompose_best_effort()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var node = root.transform.Find("sheared");

            // The y axis leans 0.5 into x: the orthonormalized rotation rolls
            // atan(0.5) = 26.565 degrees around z and the lean stretches the
            // scale to sqrt(1.25).
            Assert.That(node.localPosition, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(node.localScale.x, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(node.localScale.y, Is.EqualTo(1.118034f).Within(1e-4f));
            Assert.That(node.localScale.z, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(
                Quaternion.Angle(node.localRotation, Quaternion.Euler(0f, 0f, -26.565f)),
                Is.LessThan(0.01f));
        }
    }
}
