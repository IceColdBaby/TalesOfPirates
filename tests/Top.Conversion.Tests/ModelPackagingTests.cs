using System.IO;
using NUnit.Framework;
using Top.Conversion.Models;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Tests
{
    public class ModelPackagingTests
    {
        private static readonly string Root =
            Path.Combine(Path.GetTempPath(), "top-packaging-tests");

        private string _outputRoot;
        private CaptureLog _log;

        [SetUp]
        public void SetUp()
        {
            // Per test, so one test's output cannot be mistaken for another's
            // when a failure is being read after the fact.
            _outputRoot = Path.Combine(Root, TestContext.CurrentContext.Test.Name);
            Delete(_outputRoot);
            _log = new CaptureLog();
        }

        [TearDown]
        public void TearDown()
        {
            _log.Dispose();
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

        private string ModelPath(string name, GltfPackaging packaging = GltfPackaging.Glb)
        {
            var extension = packaging == GltfPackaging.Glb ? ".glb" : ".gltf";

            return Path.Combine(_outputRoot, "models", name + extension);
        }

        private string TextureDir => Path.Combine(_outputRoot, "textures");

        private static SceneModel Load()
        {
            using var stream = File.OpenRead(Fixtures.Path("lmo/by-bd015.lmo"));
            return LmoFile.Read(stream).Model;
        }

        private PackagedModel Write(SceneModel model, string name,
            string textureSearchDir, GltfPackaging packaging = GltfPackaging.Glb)
        {
            var target = new ModelPackaging(ModelPath(name, packaging), TextureDir, packaging);
            var file = GltfExport.Model(name, model, target.TextureUriPrefix);

            return target.Write(file, model.GeometryObjects, textureSearchDir);
        }

        private PackagedModel Write(GeometryObject obj, string name,
            string textureSearchDir, int? litSubset = null)
        {
            var target = new ModelPackaging(ModelPath(name), TextureDir);
            var file = GltfExport.Object(name, obj, target.TextureUriPrefix, litSubset);

            return target.Write(file, [obj], textureSearchDir);
        }

        [Test]
        public void Writes_a_glb_and_the_pngs_its_materials_name()
        {
            var result = Write(Load(), "by-bd015", Fixtures.Path("dds"));

            Assert.That(File.Exists(result.ModelPath));
            Assert.That(result.BinPath, Is.Null);
            Assert.That(result.TexturePaths,
                Has.Some.EndsWith("010022.png"), "resolved via .bmp -> .dds swap");
            Assert.That(result.TexturePaths, Has.Some.EndsWith("010024.png"));
            // 010023/25/26/27 are not in the fixture directory: warnings, not failures.
            Assert.That(_log.Warnings, Has.Some.Contains("010025"));

            using var fs = File.OpenRead(result.ModelPath);
            var file = GltfReader.Read(fs);
            Assert.That(file.Document.Nodes.Count, Is.EqualTo(23));
        }

        [Test]
        public void Writes_embedded_gltf()
        {
            var result = Write(Load(), "by-bd015", Fixtures.Path("dds"), GltfPackaging.GltfEmbedded);

            Assert.That(result.ModelPath, Does.EndWith("by-bd015.gltf"));
            Assert.That(result.BinPath, Is.Null);

            var doc = GltfJson.Deserialize(File.ReadAllText(result.ModelPath));
            Assert.That(doc.Buffers[0].Uri,
                Does.StartWith("data:application/octet-stream;base64,"));
        }

        [Test]
        public void Writes_gltf_with_bin()
        {
            var result = Write(Load(), "by-bd015", Fixtures.Path("dds"), GltfPackaging.GltfWithBin);

            Assert.That(result.ModelPath, Does.EndWith("by-bd015.gltf"));
            Assert.That(File.Exists(result.BinPath));

            var doc = GltfJson.Deserialize(File.ReadAllText(result.ModelPath));
            Assert.That(doc.Buffers[0].Uri, Is.EqualTo("by-bd015.bin"));
            Assert.That(doc.Buffers[0].ByteLength,
                Is.EqualTo(new FileInfo(result.BinPath).Length));
        }

        [Test]
        public void Writes_a_single_object()
        {
            GeometryObject obj;
            using (var stream = File.OpenRead(Fixtures.Path("lgo/stone01.lgo")))
            {
                obj = LgoFile.Read(stream).Object;
            }

            var result = Write(obj, "stone01", Fixtures.Path("dds"));

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
            Directory.CreateDirectory(TextureDir);
            var marker = Path.Combine(TextureDir, "010022.png");
            File.WriteAllText(marker, "keep");

            Write(Load(), "by-bd015", Fixtures.Path("dds"));

            Assert.That(File.ReadAllText(marker), Is.EqualTo("keep"));
        }

        [Test]
        public void Writes_a_weapon_with_its_lit_subset_split_off()
        {
            GeometryObject obj;
            using (var stream = File.OpenRead(Fixtures.Path("lgo/01010021.lgo")))
            {
                obj = LgoFile.Read(stream).Object;
            }

            var result = Write(obj, "01010021", Fixtures.Path("bmp"), litSubset: 1);

            Assert.That(result.TexturePaths, Has.Some.EndsWith("1.png"),
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
            var result = Write(MakeTexturedObject("1.BMP"), "bmp-item", Fixtures.Path("bmp"));

            Assert.That(_log.Warnings, Is.Empty);
            Assert.That(result.TexturePaths, Has.Some.EndsWith("1.png"));
            Assert.That(File.Exists(Path.Combine(TextureDir, "1.png")));
        }

        [Test]
        public void An_uppercase_original_becomes_a_lowercase_png_the_model_points_at()
        {
            var result = Write(MakeTexturedObject("BATMAN0023.bmp"), "loud-texture", Fixtures.Path("bmp"));

            Assert.That(result.TexturePaths,
                Has.Some.EqualTo(Path.Combine(TextureDir, "batman0023.png")));

            using var fs = File.OpenRead(result.ModelPath);
            var doc = GltfReader.Read(fs).Document;

            Assert.That(doc.Images[0].Uri, Does.EndWith("/batman0023.png"));
        }

        [Test]
        public void References_that_differ_only_in_case_share_one_png()
        {
            var obj = MakeTexturedObject("1.BMP");
            obj.Materials =
            [
                obj.Materials[0],
                new MaterialTexture { Stages = [new TextureStage { FileName = "1.bmp" }] },
            ];

            var result = Write(obj, "shared-texture", Fixtures.Path("bmp"));

            Assert.That(result.TexturePaths, Is.EqualTo(new[] { Path.Combine(TextureDir, "1.png") }));
            Assert.That(_log.Warnings, Is.Empty, "the second spelling is the same texture, not a miss");
        }

        [Test]
        public void Undecodable_texture_warns_instead_of_failing()
        {
            var result = Write(MakeTexturedObject("teampk.obj"), "bad-texture", Fixtures.Path("obj"));

            Assert.That(File.Exists(result.ModelPath));
            Assert.That(result.TexturePaths, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("teampk.obj"));
        }

        [Test]
        public void Flipbook_textures_are_collected()
        {
            var obj = MakeTexturedObject("1.BMP");
            var flipbook = new TextureImageAnimation[1, 1];
            flipbook[0, 0] = new TextureImageAnimation
            {
                DataSequence = [new TextureStage { FileName = "pstone01.BMP" }],
            };
            obj.Animation = new AnimationData { TextureImage = flipbook };

            var result = Write(obj, "flipbook", Fixtures.Path("bmp"));

            Assert.That(result.TexturePaths, Has.Some.EndsWith("1.png"));
            Assert.That(result.TexturePaths, Has.Some.EndsWith("pstone01.png"));
        }

        [Test]
        public void Builder_warnings_reach_the_log()
        {
            var obj = MakeTexturedObject("1.BMP");
            obj.Materials[0].Stages =
            [
                new TextureStage { FileName = "1.BMP" },
                new TextureStage { FileName = "pstone01.BMP" }
            ];

            Write(obj, "multi-stage", Fixtures.Path("bmp"));

            Assert.That(_log.Warnings, Has.Some.Contains("multi-stage texture"));
        }

        internal static GeometryObject MakeTexturedObject(string textureFileName)
        {
            return new GeometryObject
            {
                ParentId = uint.MaxValue,
                LocalMatrix = System.Numerics.Matrix4x4.Identity,
                Materials =
                [
                    new MaterialTexture
                    {
                        Stages = [new TextureStage { FileName = textureFileName }],
                    }
                ],
                Mesh = new Mesh
                {
                    PointType = 4,
                    Vertices =
                    [
                        new System.Numerics.Vector3(0, 0, 0),
                        new System.Numerics.Vector3(1, 0, 0),
                        new System.Numerics.Vector3(0, 1, 0)
                    ],
                    Indices = [0, 1, 2],
                    Subsets = [new MeshSubset { PrimitiveCount = 1, StartIndex = 0 }],
                },
            };
        }
    }
}
