using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Top.Assets.Conversion.Pipeline;
using Top.Gltf;
using Top.Tables;
using Top.Tables.Custom;
using Top.Tables.Records;

namespace Top.Assets.Conversion.Tests.Pipeline
{
    public class RigConverterTests
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

        private static CharacterInfoRecord Character(int id, int actionId)
        {
            return new CharacterInfoRecord
            {
                Id = id,
                Name = $"character {id}",
                Model = 1,
                ActionId = actionId,
                ModalType = CharacterModalType.MainCharacter,
                Parts = new int[5],
            };
        }

        private RigConverter Converter(bool overwrite = true, params CharacterInfoRecord[] characters)
        {
            var tables = new ClientTables(
                new Table<CharacterInfoRecord>([..characters]),
                null, null, FakeClient.ActionSet(7));

            return new RigConverter(_client.Settings(overwrite), tables);
        }

        [Test]
        public void Converts_a_skeleton_with_the_clips_its_characters_play()
        {
            _client.AddSkeleton("lab/0001.lab");

            var artifact = Converter(true, Character(10, actionId: 7)).Convert(1);

            Assert.That(artifact.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(artifact.Kind, Is.EqualTo("Character"));
            Assert.That(artifact.ModelPath, Is.EqualTo(_client.ConvertedRig("0001")));

            using var stream = File.OpenRead(artifact.ModelPath);
            Assert.That(GltfReader.Read(stream).Document.Animations, Is.Not.Empty);
        }

        [Test]
        public void Characters_disagreeing_on_the_action_set_are_reported_and_the_first_wins()
        {
            _client.AddSkeleton("lab/0001.lab");

            Converter(true, Character(10, actionId: 7), Character(11, actionId: 9)).Convert(1);

            Assert.That(_log.Warnings, Has.Some.Contains("action set 9"));
            Assert.That(_log.Warnings, Has.Some.Contains("keeping 7"));
        }

        [Test]
        public void A_skeleton_no_character_uses_converts_without_clips()
        {
            _client.AddSkeleton("lab/0001.lab");

            var artifact = Converter().Convert(1);

            Assert.That(artifact.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(_log.Warnings, Has.Some.Contains("no characterinfo row uses model 1"));
        }

        [Test]
        public void A_skeleton_file_that_is_not_named_after_a_model_still_converts()
        {
            var path = _client.AddSkeleton("lab/0001.lab", "hero.lab");

            var artifact = Converter().Convert(path);

            Assert.That(artifact.Name, Is.EqualTo("hero"));
            Assert.That(_log.Warnings, Has.Some.Contains("'hero' is not a skeleton model id"));
        }

        [Test]
        public void A_missing_skeleton_fails()
        {
            Assert.That(Converter().Convert(1), Is.Null);
            Assert.That(_log.Errors, Has.Some.Contains("0001.lab"));
        }

        [Test]
        public void Output_already_in_place_is_skipped()
        {
            _client.AddSkeleton("lab/0001.lab");
            Converter().Convert(1);

            Assert.That(Converter(overwrite: false).Convert(1).Outcome,
                Is.EqualTo(ConversionOutcome.Skipped));
        }

        [Test]
        public void A_skeleton_named_twice_in_one_run_converts_once()
        {
            _client.AddSkeleton("lab/0001.lab");
            var converter = Converter();

            converter.Convert(1);

            Assert.That(converter.Convert(1).Outcome, Is.EqualTo(ConversionOutcome.Skipped));
        }
    }
}
