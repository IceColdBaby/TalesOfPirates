using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Tests.Pipeline
{
    public class TableConverterTests
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

        private static SceneObjectInfoRecord Row(int id, string fileName)
        {
            return new SceneObjectInfoRecord { Id = id, Name = fileName };
        }

        private TableConverter Converter(bool overwrite = true, params SceneObjectInfoRecord[] rows)
        {
            var tables = new ClientTables(null, null,
                new Top.Legacy.Tables.Table<SceneObjectInfoRecord>([..rows]), null);

            return new TableConverter(_client.Settings(overwrite), tables);
        }

        private string TablePath => Path.Combine(_client.OutputRoot, "tables", "sceneobjects.json");

        private List<SceneObjectEntry> Emitted()
        {
            using var stream = File.OpenRead(TablePath);

            return new TableFormat().Read<SceneObjectEntry>(stream);
        }

        [Test]
        public void Every_row_emits_mapped_to_its_id_and_converted_model_path()
        {
            var result = Converter(rows:
            [
                Row(42, "Stone01.lgo"),
                Row(7, string.Empty),
            ]).ConvertAll().Single();

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));

            var entries = Emitted();

            Assert.That(entries.Select(entry => entry.Id), Is.EqualTo(new[] { 42, 7 }),
                "every row emits, model file or not");
            Assert.That(entries[0].ModelPath, Is.EqualTo("models/scene/stone01.glb"),
                "resolved by the output layout convention, no model converted first");
            Assert.That(entries[1].ModelPath, Is.Null, "lights, sounds and fog carry no path");
        }

        [Test]
        public void The_whole_row_emits()
        {
            var row = Row(42, "Stone01.lgo");

            row.DisplayName = "stone";
            row.AttachEffectId = 5;
            row.EnableEnvLight = true;
            row.EnablePointLight = true;
            row.Style = 1;
            row.Flag = 2;
            row.SizeFlag = 3;
            row.ShadeFlag = true;
            row.IsReallyBig = true;

            Converter(rows: row).ConvertAll().Single();

            var entry = Emitted().Single();

            Assert.That(entry.DisplayName, Is.EqualTo("stone"));
            Assert.That(entry.AttachEffectId, Is.EqualTo(5));
            Assert.That(entry.EnableEnvLight, Is.True);
            Assert.That(entry.EnablePointLight, Is.True);
            Assert.That(entry.Style, Is.EqualTo(1));
            Assert.That(entry.Flag, Is.EqualTo(2));
            Assert.That(entry.SizeFlag, Is.EqualTo(3));
            Assert.That(entry.ShadeFlag, Is.True);
            Assert.That(entry.IsReallyBig, Is.True);
        }

        [Test]
        public void The_type_selects_the_variation_the_row_emits_as()
        {
            var fading = Row(1, "Tree01.lgo");
            fading.FadeObjSeq = new[] { 1, 2 };
            fading.FadeCoefficient = 0.5f;

            var light = new SceneObjectInfoRecord
            {
                Id = 2, Name = string.Empty, Type = 3, PointColor = new[] { 255, 128, 0 },
                PointLightRange = 500, PointLightAttenuation = 0.7f, PointLightAnimCtrlId = 9,
            };
            var ambient = new SceneObjectInfoRecord
            {
                Id = 3, Name = string.Empty, Type = 4, EnvColor = new[] { 10, 20, 30 },
            };
            var fog = new SceneObjectInfoRecord
            {
                Id = 4, Name = string.Empty, Type = 5, FogColor = new[] { 1, 2, 3 },
            };
            var sound = new SceneObjectInfoRecord
            {
                Id = 5, Name = string.Empty, Type = 6, EnvSound = "wave.wav", EnvSoundDistance = 900,
            };

            Converter(rows: [fading, light, ambient, fog, sound]).ConvertAll().Single();

            var entries = Emitted();

            var faded = (FadeEntry)entries[0];

            Assert.That(faded.Sequence, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(faded.Coefficient, Is.EqualTo(0.5f));

            var pointLight = (PointLightEntry)entries[1];

            Assert.That(pointLight.Type, Is.EqualTo(3));
            Assert.That(pointLight.Color, Is.EqualTo(new[] { 255, 128, 0 }));
            Assert.That(pointLight.Range, Is.EqualTo(500));
            Assert.That(pointLight.Attenuation, Is.EqualTo(0.7f));
            Assert.That(pointLight.AnimationId, Is.EqualTo(9));
            Assert.That(((AmbientLightEntry)entries[2]).Color, Is.EqualTo(new[] { 10, 20, 30 }));
            Assert.That(((FogEntry)entries[3]).Color, Is.EqualTo(new[] { 1, 2, 3 }));

            var emitted = (SoundEntry)entries[4];

            Assert.That(emitted.Sound, Is.EqualTo("wave.wav"));
            Assert.That(emitted.Distance, Is.EqualTo(900));
        }

        [Test]
        public void An_ordinary_row_without_fade_data_emits_as_the_base()
        {
            Converter(rows: Row(42, "Stone01.lgo")).ConvertAll().Single();

            Assert.That(Emitted().Single().GetType(), Is.EqualTo(typeof(SceneObjectEntry)));
        }

        [Test]
        public void A_missing_catalog_fails()
        {
            var settings = _client.Settings();
            var tables = new ClientTables(null, null, null, null);

            var result = new TableConverter(settings, tables).ConvertAll().Single();

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(File.Exists(TablePath), Is.False);
            Assert.That(_log.Errors, Has.Some.Contains("sceneobjects"));
        }

        [Test]
        public void A_table_already_in_the_tree_is_skipped_without_overwrite()
        {
            Converter(rows: Row(42, "Stone01.lgo")).ConvertAll().Single();

            var result = Converter(overwrite: false, Row(42, "Stone01.lgo"), Row(7, string.Empty))
                .ConvertAll().Single();

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Skipped));
            Assert.That(Emitted(), Has.Count.EqualTo(1));
        }

        [Test]
        public void A_batch_is_the_one_table_unit()
        {
            var results = Converter(rows: Row(42, "Stone01.lgo")).ConvertAll().ToList();

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Name, Is.EqualTo("sceneobjects"));
            Assert.That(results[0].Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(results[0].Artifacts, Is.Empty);
        }
    }
}
