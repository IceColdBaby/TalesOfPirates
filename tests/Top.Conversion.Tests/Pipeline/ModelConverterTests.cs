using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Conversion.Gltf;

namespace Top.Conversion.Tests.Pipeline
{
    public class ModelConverterTests
    {
        private FakeClient _client;
        private CaptureLog _log;

        [SetUp]
        public void SetUp()
        {
            _client = new FakeClient();
            _log = new CaptureLog();
        }

        [TearDown]
        public void TearDown()
        {
            _log.Dispose();
            _client.Dispose();
        }

        private ModelConverter Converter(bool overwrite = true)
        {
            return new ModelConverter(_client.Settings(overwrite));
        }

        [Test]
        public void Converts_an_object_into_the_content_family_its_folder_names()
        {
            var source = _client.AddModel("scene", "lgo/stone01.lgo");

            var artifact = Converter().Convert(source);

            Assert.That(artifact.Kind, Is.EqualTo("scene"));
            Assert.That(artifact.Name, Is.EqualTo("stone01"));
            Assert.That(artifact.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(artifact.ModelPath, Is.EqualTo(_client.Converted("scene", "stone01")));
            Assert.That(File.Exists(artifact.ModelPath));
        }

        [Test]
        public void Models_of_a_kind_land_beside_each_other_as_single_files()
        {
            var converter = Converter();
            converter.Convert(_client.AddModel("scene", "lgo/stone01.lgo"));
            converter.Convert(_client.AddModel("scene", "lgo/dirk.lgo"));

            var kindDir = Path.Combine(_client.OutputRoot, "models", "scene");

            Assert.That(Directory.GetFiles(kindDir).Select(Path.GetFileName),
                Is.EquivalentTo(new[] { "stone01.glb", "dirk.glb" }));
            Assert.That(Directory.GetDirectories(kindDir), Is.Empty);
        }

        [Test]
        public void A_mixed_case_original_lands_lowercase()
        {
            var source = _client.AddModel("scene", "lgo/stone01.lgo", "MY_BD001.lgo");

            var artifact = Converter().Convert(source);

            Assert.That(artifact.Name, Is.EqualTo("my_bd001"));
            Assert.That(artifact.ModelPath, Is.EqualTo(_client.Converted("scene", "my_bd001")));
            Assert.That(Directory.GetFiles(Path.Combine(_client.OutputRoot, "models", "scene"))
                .Select(Path.GetFileName), Is.EqualTo(new[] { "my_bd001.glb" }));
        }

        [Test]
        public void Converts_a_scene_model()
        {
            var source = _client.AddModel("scene", "lmo/by-bd015.lmo");
            _client.AddTextures("scene", "dds");

            var artifact = Converter().Convert(source);

            Assert.That(artifact.TexturePaths, Has.Some.EndsWith("010022.png"));

            using var stream = File.OpenRead(artifact.ModelPath);
            Assert.That(GltfReader.Read(stream).Document.Nodes, Has.Count.EqualTo(23));
        }

        [Test]
        public void Writes_textures_where_the_glTF_points_at_them()
        {
            var source = _client.AddModel("scene", "lmo/by-bd015.lmo");
            _client.AddTextures("scene", "dds");

            var artifact = Converter().Convert(source);
            var png = _client.ConvertedTexture("scene", "010022.png");

            Assert.That(File.Exists(png));
            Assert.That(artifact.TexturePaths, Has.Some.EqualTo(png));

            using var stream = File.OpenRead(artifact.ModelPath);
            var document = GltfReader.Read(stream).Document;

            Assert.That(document.Images[0].Uri, Does.StartWith("../../textures/scene/"));

            var modelDir = Path.GetDirectoryName(artifact.ModelPath);
            var resolved = document.Images
                .Select(image => Path.GetFullPath(Path.Combine(modelDir, image.Uri)));

            Assert.That(resolved, Has.Some.EqualTo(Path.GetFullPath(png)));
        }

        [Test]
        public void An_item_object_gets_its_lit_shell()
        {
            var source = _client.AddModel("item", "lgo/01010021.lgo");

            var artifact = Converter().Convert(source);

            using var stream = File.OpenRead(artifact.ModelPath);
            var document = GltfReader.Read(stream).Document;

            Assert.That(document.Nodes, Has.Some.Matches<GltfNode>(node => node.Name.EndsWith("_lit")));
        }

        [Test]
        public void A_skinned_object_binds_to_the_skeleton_its_name_points_at()
        {
            _client.AddSkeleton("lab/0724.lab");
            var source = _client.AddModel("character", "lgo/0724000000.lgo");

            var artifact = Converter().Convert(source);

            using var stream = File.OpenRead(artifact.ModelPath);
            Assert.That(GltfReader.Read(stream).Document.Skins, Is.Not.Empty);
        }

        [Test]
        public void A_skinned_object_with_no_skeleton_converts_rigid()
        {
            var source = _client.AddModel("character", "lgo/0724000000.lgo");

            var artifact = Converter().Convert(source);

            Assert.That(artifact.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(_log.Warnings, Has.Some.Contains("converting rigid"));
        }

        [Test]
        public void A_path_outside_the_client_model_folders_has_no_content_family()
        {
            var stray = Path.Combine(_client.ClientRoot, "stone01.lgo");
            File.WriteAllBytes(stray, new byte[] { 0 });

            Assert.That(Converter().Convert(stray), Is.Null);
            Assert.That(_log.Errors, Has.Some.Contains("cannot derive model kind"));
        }

        [Test]
        public void A_missing_file_fails_without_throwing()
        {
            var missing = Path.Combine(_client.ClientRoot, "model", "scene", "nothing.lgo");

            Assert.That(Converter().Convert(missing), Is.Null);
            Assert.That(_log.Errors, Has.Some.Contains("nothing.lgo"));
        }

        [Test]
        public void Unreadable_originals_fail_without_throwing()
        {
            var source = _client.AddModel("scene", "obj/teampk.obj", "broken.lgo");

            Assert.That(Converter().Convert(source), Is.Null);
            Assert.That(_log.Errors, Has.Some.Contains("failed to parse"));
        }

        [Test]
        public void Output_already_in_place_is_skipped()
        {
            var source = _client.AddModel("scene", "lgo/stone01.lgo");
            Converter().Convert(source);

            var artifact = Converter(overwrite: false).Convert(source);

            Assert.That(artifact.Outcome, Is.EqualTo(ConversionOutcome.Skipped));
            Assert.That(artifact.ModelPath, Is.EqualTo(_client.Converted("scene", "stone01")));
            Assert.That(artifact.TexturePaths, Is.Empty);
        }

        [Test]
        public void Overwriting_writes_output_that_is_already_in_place()
        {
            var source = _client.AddModel("scene", "lgo/stone01.lgo");
            Converter().Convert(source);

            Assert.That(Converter().Convert(source).Outcome, Is.EqualTo(ConversionOutcome.Converted));
        }

        [Test]
        public void A_source_named_twice_in_one_run_converts_once()
        {
            var source = _client.AddModel("scene", "lgo/stone01.lgo");
            var converter = Converter();

            converter.Convert(source);

            Assert.That(converter.Convert(source).Outcome, Is.EqualTo(ConversionOutcome.Skipped));
        }
    }
}
