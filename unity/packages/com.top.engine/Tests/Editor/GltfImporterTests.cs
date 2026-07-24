using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor.Tests
{
    public class GltfImporterTests
    {
        private const string TempDir = "Assets/TempGltfTests";
        private const string GlbPath = TempDir + "/tri.glb";

        [OneTimeSetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(TempDir);
            var doc = new GltfDocument
            {
                Scene = 0,
                Scenes = new List<GltfScene> { new GltfScene { Nodes = new List<int> { 0 } } },
                Nodes = new List<GltfNode> { new GltfNode { Name = "tri", Mesh = 0 } },
                Meshes = new List<GltfMesh> { new GltfMesh { Name = "tri_mesh" } },
                Materials = new List<GltfMaterial> { new GltfMaterial { Name = "tri_mat" } },
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
                new GltfAnimation
                {
                    Name = "default",
                    Samplers =
                    {
                        new GltfAnimationSampler
                        {
                            Input = input, Output = output, Interpolation = "LINEAR",
                        },
                    },
                    Channels =
                    {
                        new GltfAnimationChannel
                        {
                            Sampler = 0,
                            Target = { Node = 0, Path = "rotation" },
                        },
                    },
                },
            };
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
        public void Imports_mesh_sub_asset_with_stable_name()
        {
            var mesh = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
                .OfType<UnityEngine.Mesh>().Single();

            Assert.That(mesh.name, Is.EqualTo("tri_mesh"));
            Assert.That(mesh.vertexCount, Is.EqualTo(3));
        }

        [Test]
        public void Positions_are_z_negated_on_import()
        {
            var mesh = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
                .OfType<UnityEngine.Mesh>().Single();

            Assert.That(mesh.vertices[0],
                Is.EqualTo(new UnityEngine.Vector3(1, 2, -3)));
        }

        [Test]
        public void Winding_is_reversed_on_import()
        {
            var mesh = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
                .OfType<UnityEngine.Mesh>().Single();

            Assert.That(mesh.triangles, Is.EqualTo(new[] { 0, 2, 1 }));
        }

        [Test]
        public void Main_asset_is_preview_hierarchy()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;

            Assert.That(root, Is.Not.Null);
            Assert.That(root.transform.childCount, Is.EqualTo(1));
            Assert.That(root.transform.GetChild(0).name, Is.EqualTo("tri"));
        }

        [Test]
        public void Renderer_binds_remapped_material()
        {
            var materialPath = TempDir + "/remap_test.mat";
            var material = new Material(Shader.Find("Top/Legacy"));
            AssetDatabase.CreateAsset(material, materialPath);
            var importer = AssetImporter.GetAtPath(GlbPath);
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), "tri_mat"),
                material);
            AssetDatabase.WriteImportSettingsIfDirty(GlbPath);
            AssetDatabase.ImportAsset(GlbPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var renderer = root.GetComponentInChildren<MeshRenderer>();
            Assert.That(renderer.sharedMaterials, Is.EqualTo(new[] { material }));
        }

        [Test]
        public void Imports_animation_as_legacy_clip()
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(GlbPath)
                .OfType<AnimationClip>().Single();

            Assert.That(clip.legacy, Is.True);
            Assert.That(clip.name, Is.EqualTo("default"));
            var bindings = AnimationUtility.GetCurveBindings(clip);
            Assert.That(bindings.Length, Is.EqualTo(4));
            Assert.That(bindings.Select(b => b.path), Is.All.EqualTo(string.Empty));
        }

        [Test]
        public void Wires_animation_component_on_target_node()
        {
            var root = AssetDatabase.LoadMainAssetAtPath(GlbPath) as GameObject;
            var animation = root.transform.Find("tri").GetComponent<Animation>();

            Assert.That(animation, Is.Not.Null);
            Assert.That(animation.clip.name, Is.EqualTo("default"));
            Assert.That(animation.playAutomatically, Is.True);
            Assert.That(animation.cullingType,
                Is.EqualTo(AnimationCullingType.BasedOnRenderers));
        }
    }
}
