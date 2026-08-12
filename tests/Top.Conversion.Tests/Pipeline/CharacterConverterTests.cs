using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Tests.Pipeline
{
    public class CharacterConverterTests
    {
        private const int PlayerModel = 1;
        private const int MonsterModel = 724;

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

        private static CharacterInfoRecord Character(int id, CharacterModalType modal, int model,
            params int[] parts)
        {
            // characterinfo carries eight part columns; only the first five
            // are equipment slots.
            var slots = new int[8];
            parts.CopyTo(slots, 0);

            return new CharacterInfoRecord
            {
                Id = id,
                Name = $"character {id}",
                ModalType = modal,
                Model = model,
                SuitId = 0,
                ActionId = 7,
                Parts = slots,
            };
        }

        private static ItemInfoRecord Wearable(int id, int framework, string module)
        {
            var columns = new[] { "0", "0", "0", "0", "0" };
            columns[framework + 1] = module;

            return new ItemInfoRecord
            {
                Id = id,
                Name = $"item {id}",
                Type = ItemType.Clothing,
                Modules = columns,
            };
        }

        private CharacterConverter Converter(IEnumerable<CharacterInfoRecord> characters,
            IEnumerable<ItemInfoRecord> items = null, bool overwrite = true)
        {
            var settings = _client.Settings(overwrite);
            var tables = new ClientTables(
                new Table<CharacterInfoRecord>(characters.ToList()),
                new Table<ItemInfoRecord>((items ?? []).ToList()),
                null,
                FakeClient.ActionSet(7));

            var models = new ModelConverter(settings);

            return new CharacterConverter(settings, tables,
                new RigConverter(settings, tables),
                new ItemConverter(settings, tables, models));
        }

        [Test]
        public void A_player_character_keeps_its_rig_and_its_parts_apart()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddModel("character", "lgo/0724000000.lgo", "0001000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500);
            var result = Converter([row], [Wearable(500, PlayerModel, "0001000000")])
                .Convert(10);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(result.Name, Is.EqualTo("0010"));
            Assert.That(result.Model, Is.Null, "a player is composed, not merged");
            Assert.That(result.Rig.ModelPath, Is.EqualTo(_client.ConvertedRig("0001")));
            Assert.That(result.Parts, Has.Count.EqualTo(1));
            Assert.That(result.Parts[0].ModuleArtifacts[PlayerModel].ModelPath,
                Is.EqualTo(_client.Converted("Character", "0001000000")));
        }

        [Test]
        public void A_player_character_wears_only_its_first_five_part_slots()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddModel("character", "lgo/0724000000.lgo", "0001000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel,
                500, 0, 0, 0, 0, 501, 501, 501);
            var items = new[]
            {
                Wearable(500, PlayerModel, "0001000000"),
                Wearable(501, PlayerModel, "0001000000"),
            };

            var result = Converter([row], items).Convert(10);

            Assert.That(result.Parts, Has.Count.EqualTo(1),
                "slots past the fifth are monster meshes, not equipment");
        }

        [Test]
        public void A_player_part_naming_no_item_is_reported_and_left_out()
        {
            _client.AddSkeleton("lab/0001.lab");
            _client.AddModel("character", "lgo/0724000000.lgo", "0001000000.lgo");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500, 999);
            var result = Converter([row], [Wearable(500, PlayerModel, "0001000000")])
                .Convert(10);

            Assert.That(result.Parts, Has.Count.EqualTo(1));
            Assert.That(_log.Warnings, Has.Some.Contains("unknown item 999"));
        }

        [Test]
        public void A_player_character_whose_parts_all_fail_fails()
        {
            _client.AddSkeleton("lab/0001.lab");

            var row = Character(10, CharacterModalType.MainCharacter, PlayerModel, 500);
            var result = Converter([row], [Wearable(500, PlayerModel, "missing")])
                .Convert(10);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("no part of character 10 loaded"));
        }

        [Test]
        public void A_monster_is_merged_into_one_model_named_after_its_skeleton()
        {
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var row = Character(20, CharacterModalType.Other, MonsterModel, 1);
            var result = Converter([row]).Convert(20);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Converted));
            Assert.That(result.Name, Is.EqualTo("0724"));
            Assert.That(result.Rig, Is.Null, "the skeleton rides inside the merged model");
            Assert.That(result.Model.ModelPath, Is.EqualTo(_client.Converted("Character", "0724")));
        }

        [Test]
        public void Monsters_sharing_a_skeleton_merge_once()
        {
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var rows = new[]
            {
                Character(20, CharacterModalType.Other, MonsterModel, 1),
                Character(21, CharacterModalType.Other, MonsterModel, 1),
            };

            var results = Converter(rows).ConvertAll().ToList();

            Assert.That(results.Select(result => result.Outcome),
                Is.EqualTo(new[] { ConversionOutcome.Converted, ConversionOutcome.Skipped }));
            Assert.That(results[1].Model.ModelPath, Is.EqualTo(results[0].Model.ModelPath));
        }

        [Test]
        public void A_monster_with_no_meshes_fails()
        {
            _client.AddSkeleton("lab/0724.lab");

            var row = Character(20, CharacterModalType.Other, MonsterModel, 1);
            var result = Converter([row]).Convert(20);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("no part of model 724 loaded"));
        }

        [Test]
        public void A_monster_with_no_skeleton_fails()
        {
            _client.AddModel("character", "lgo/0724000000.lgo");

            var row = Character(20, CharacterModalType.Other, MonsterModel, 1);
            var result = Converter([row]).Convert(20);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("0724.lab"));
        }

        [Test]
        public void Characters_that_are_neither_are_skipped()
        {
            var row = Character(30, CharacterModalType.Boat, MonsterModel);

            var result = Converter([row]).Convert(30);

            Assert.That(result.Outcome, Is.EqualTo(ConversionOutcome.Skipped));
            Assert.That(result.Artifacts, Is.Empty);
        }

        [Test]
        public void An_unknown_id_fails()
        {
            Assert.That(Converter([]).Convert(10).Outcome,
                Is.EqualTo(ConversionOutcome.Failed));
            Assert.That(_log.Errors, Has.Some.Contains("no characterinfo row 10"));
        }

        [Test]
        public void Converting_a_skeleton_covers_every_character_on_it()
        {
            _client.AddSkeleton("lab/0724.lab");
            _client.AddModel("character", "lgo/0724000000.lgo");

            var rows = new[]
            {
                Character(20, CharacterModalType.Other, MonsterModel, 1),
                Character(21, CharacterModalType.Other, MonsterModel, 1),
                Character(22, CharacterModalType.Other, model: 99, parts: 1),
            };

            var results = Converter(rows).ConvertModel(MonsterModel).ToList();

            Assert.That(results.Select(result => result.Id), Is.EqualTo(new[] { 20, 21 }));
        }

        [Test]
        public void A_skeleton_no_character_uses_converts_nothing()
        {
            var results = Converter([])
                .ConvertModel(MonsterModel)
                .ToList();

            Assert.That(results, Is.Empty);
            Assert.That(_log.Warnings, Has.Some.Contains("no characterinfo row uses model 724"));
        }
    }
}
