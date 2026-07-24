using System.IO;
using NUnit.Framework;
using Top.Assets.Conversion;
using Top.Gltf;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
{
    public class ConversionTests
    {
        private static readonly string Root =
            Path.Combine(Path.GetTempPath(), "top-convert-tests");

        private string _outputRoot;

        [SetUp]
        public void SetUp()
        {
            // Per test, so one test's output cannot be mistaken for another's
            // when a failure is being read after the fact.
            _outputRoot = Path.Combine(Root, TestContext.CurrentContext.Test.Name);
            Delete(_outputRoot);
        }

        [TearDown]
        public void TearDown()
        {
            Delete(_outputRoot);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Delete(Root);
        }

        private static void Delete(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static SceneModel Load()
        {
            using var stream = File.OpenRead(Fixtures.Path("lmo/by-bd015.lmo"));
            return LmoFile.Read(stream).Model;
        }

        [Test]
        public void Converts_model_to_glb_and_pngs()
        {
            var result = ModelConversion.Convert(
                Load(), "by-bd015",
                Fixtures.Path("dds"),
                Path.Combine(_outputRoot, "scene"),
                Path.Combine(_outputRoot, "textures"));

            Assert.That(File.Exists(result.ModelPath));
            Assert.That(result.BinPath, Is.Null);
            Assert.That(result.TexturePngPaths,
                Has.Some.EndsWith("010022.png"), "resolved via .bmp -> .dds swap");
            Assert.That(result.TexturePngPaths, Has.Some.EndsWith("010024.png"));
            // 010023/25/26/27 are not in the fixture directory: warnings, not failures.
            Assert.That(result.Warnings, Has.Some.Contains("010025"));

            using var fs = File.OpenRead(result.ModelPath);
            var file = GltfReader.Read(fs);
            Assert.That(file.Document.Nodes.Count, Is.EqualTo(23));
        }

        [Test]
        public void Writes_embedded_gltf()
        {
            var result = ModelConversion.Convert(
                Load(), "by-bd015",
                Fixtures.Path("dds"),
                Path.Combine(_outputRoot, "scene"),
                Path.Combine(_outputRoot, "textures"),
                GltfPackaging.GltfEmbedded);

            Assert.That(result.ModelPath, Does.EndWith("by-bd015.gltf"));
            Assert.That(result.BinPath, Is.Null);

            var doc = GltfJson.Deserialize(File.ReadAllText(result.ModelPath));
            Assert.That(doc.Buffers[0].Uri,
                Does.StartWith("data:application/octet-stream;base64,"));
        }

        [Test]
        public void Writes_gltf_with_bin()
        {
            var result = ModelConversion.Convert(
                Load(), "by-bd015",
                Fixtures.Path("dds"),
                Path.Combine(_outputRoot, "scene"),
                Path.Combine(_outputRoot, "textures"),
                GltfPackaging.GltfWithBin);

            Assert.That(result.ModelPath, Does.EndWith("by-bd015.gltf"));
            Assert.That(File.Exists(result.BinPath));

            var doc = GltfJson.Deserialize(File.ReadAllText(result.ModelPath));
            Assert.That(doc.Buffers[0].Uri, Is.EqualTo("by-bd015.bin"));
            Assert.That(doc.Buffers[0].ByteLength,
                Is.EqualTo(new FileInfo(result.BinPath).Length));
        }

        [Test]
        public void Converts_lgo_object()
        {
            GeometryObject obj;
            using (var stream = File.OpenRead(Fixtures.Path("lgo/stone01.lgo")))
            {
                obj = LgoFile.Read(stream).Object;
            }

            var result = ModelConversion.Convert(
                obj, "stone01",
                Fixtures.Path("dds"),
                Path.Combine(_outputRoot, "scene"),
                Path.Combine(_outputRoot, "textures"));

            Assert.That(File.Exists(result.ModelPath));

            using var fs = File.OpenRead(result.ModelPath);
            var doc = GltfReader.Read(fs).Document;
            Assert.That(doc.Nodes.Count,
                Is.EqualTo(1 + (obj.Helper?.Meshes?.Length ?? 0)));
            Assert.That(doc.Nodes[0].Name, Is.EqualTo($"geom_{obj.Id}"));
        }

        [Test]
        public void Existing_pngs_are_not_rewritten()
        {
            var textureDir = Path.Combine(_outputRoot, "textures");
            Directory.CreateDirectory(textureDir);
            var marker = Path.Combine(textureDir, "010022.png");
            File.WriteAllText(marker, "keep");

            ModelConversion.Convert(
                Load(), "by-bd015", Fixtures.Path("dds"),
                Path.Combine(_outputRoot, "scene"), textureDir);

            Assert.That(File.ReadAllText(marker), Is.EqualTo("keep"));
        }

        [Test]
        public void Converts_weapon_lgo_with_lit_subset()
        {
            GeometryObject obj;
            using (var stream = File.OpenRead(Fixtures.Path("lgo/01010021.lgo")))
            {
                obj = LgoFile.Read(stream).Object;
            }

            var result = ModelConversion.Convert(
                obj, "01010021",
                Fixtures.Path("bmp"),
                Path.Combine(_outputRoot, "models"),
                Path.Combine(_outputRoot, "textures"),
                litSubset: 1);

            Assert.That(result.TexturePngPaths, Has.Some.EndsWith("1.png"),
                "glow placeholder 1.BMP decodes");

            using var fs = File.OpenRead(result.ModelPath);
            var doc = GltfReader.Read(fs).Document;
            var main = doc.Nodes.Find(n => n.Name == $"geom_{obj.Id}");
            var lit = doc.Nodes.FindIndex(n => n.Name == $"geom_{obj.Id}_lit");

            Assert.That(main.Children, Is.EqualTo(new[] { lit }));
            Assert.That(doc.Meshes[main.Mesh.Value].Primitives, Has.Count.EqualTo(1));

            var litPrimitive = doc.Meshes[doc.Nodes[lit].Mesh.Value].Primitives[0];
            Assert.That(doc.Materials[litPrimitive.Material.Value].Name,
                Is.EqualTo($"01010021_{obj.Id}_1"), "remap contract keeps subset numbering");
        }

        [Test]
        public void Bmp_texture_converts_to_png()
        {
            var result = ModelConversion.Convert(
                MakeTexturedObject("1.BMP"), "bmp-item",
                Fixtures.Path("bmp"),
                Path.Combine(_outputRoot, "models"),
                Path.Combine(_outputRoot, "textures"));

            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.TexturePngPaths, Has.Some.EndsWith("1.png"));
            Assert.That(File.Exists(Path.Combine(_outputRoot, "textures", "1.png")));
        }

        [Test]
        public void Undecodable_texture_warns_instead_of_failing()
        {
            var result = ModelConversion.Convert(
                MakeTexturedObject("teampk.obj"), "bad-texture",
                Fixtures.Path("obj"),
                Path.Combine(_outputRoot, "models"),
                Path.Combine(_outputRoot, "textures"));

            Assert.That(File.Exists(result.ModelPath));
            Assert.That(result.TexturePngPaths, Is.Empty);
            Assert.That(result.Warnings, Has.Some.Contains("teampk.obj"));
        }

        [Test]
        public void Flipbook_textures_are_collected()
        {
            var obj = MakeTexturedObject("1.BMP");
            var flipbook = new TextureImageAnimation[1, 1];
            flipbook[0, 0] = new TextureImageAnimation
            {
                DataSequence = new[] { new TextureStage { FileName = "pstone01.BMP" } },
            };
            obj.Animation = new AnimationData { TextureImage = flipbook };

            var result = ModelConversion.Convert(
                obj, "flipbook", Fixtures.Path("bmp"),
                Path.Combine(_outputRoot, "models"),
                Path.Combine(_outputRoot, "textures"));

            Assert.That(result.TexturePngPaths, Has.Some.EndsWith("1.png"));
            Assert.That(result.TexturePngPaths, Has.Some.EndsWith("pstone01.png"));
        }

        private static GeometryObject MakeTexturedObject(string textureFileName)
        {
            return new GeometryObject
            {
                LocalMatrix = System.Numerics.Matrix4x4.Identity,
                Materials = new[]
                {
                    new MaterialTexture
                    {
                        Stages = new[] { new TextureStage { FileName = textureFileName } },
                    },
                },
                Mesh = new Mesh
                {
                    PointType = 4,
                    Vertices = new[]
                    {
                        new System.Numerics.Vector3(0, 0, 0),
                        new System.Numerics.Vector3(1, 0, 0),
                        new System.Numerics.Vector3(0, 1, 0),
                    },
                    Indices = new uint[] { 0, 1, 2 },
                    Subsets = new[] { new MeshSubset { PrimitiveCount = 1, StartIndex = 0 } },
                },
            };
        }

        [Test]
        public void Emitted_gltf_contains_no_extras()
        {
            var conversion = GeometryObjectGltfMapper.Map(Load(), "by-bd015");
            var json = GltfJson.Serialize(conversion.Document, indented: false);

            Assert.That(json, Does.Not.Contain("\"extras\""));
        }
    }
}
