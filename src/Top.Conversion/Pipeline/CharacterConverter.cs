using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Top.Conversion.Models;
using Top.Conversion.Models.Gltf;
using Top.Logging;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// A characterinfo unit. A player character stays in pieces - a rig its
    /// equipment binds to - while a monster is merged into a single glTF that
    /// carries its own skeleton.
    /// </summary>
    public class CharacterResult : UnitResult
    {
        public CharacterResult(int id, string name, ConversionOutcome outcome,
            ModelArtifact rig, ModelArtifact model, IReadOnlyList<ItemResult> parts)
            : base(id, name, outcome)
        {
            Rig = rig;
            Model = model;
            Parts = parts;
        }

        /// <summary>
        /// The skeleton a player character animates on, null for a monster.
        /// </summary>
        public ModelArtifact Rig { get; }

        /// <summary>
        /// The merged glTF a monster is, null for a player character.
        /// </summary>
        public ModelArtifact Model { get; }

        /// <summary>
        /// The equipment a player character wears, one result per occupied
        /// slot, each converted for that character's framework only.
        /// </summary>
        public IReadOnlyList<ItemResult> Parts { get; }

        public override IEnumerable<ModelArtifact> Artifacts
        {
            get
            {
                if (Rig != null)
                {
                    yield return Rig;
                }

                if (Model != null)
                {
                    yield return Model;
                }

                foreach (var artifact in Parts.SelectMany(part => part.Artifacts))
                {
                    yield return artifact;
                }
            }
        }
    }

    /// <summary>
    /// Converts characterinfo units. Characters that share a skeleton share
    /// its rig and, when they are monsters, its merged model: the first one
    /// converts, the rest read as skipped.
    /// </summary>
    public class CharacterConverter
    {
        private const int EquipmentSlots = 5;

        private static readonly IReadOnlyList<ItemResult> NoParts = Array.Empty<ItemResult>();

        private readonly ConversionSettings _settings;
        private readonly ClientTables _tables;
        private readonly RigConverter _rigs;
        private readonly ItemConverter _items;

        private readonly ArtifactMemo _merged = new ArtifactMemo();

        public CharacterConverter(ConversionSettings settings, ClientTables tables, RigConverter rigs,
            ItemConverter items)
        {
            _settings = settings;
            _tables = tables;
            _rigs = rigs;
            _items = items;
        }

        public CharacterResult Convert(int id)
        {
            if (_tables.Characters == null || !_tables.Characters.TryGetById(id, out var row))
            {
                Log.Error($"no characterinfo row {id}");

                return new CharacterResult(id, null, ConversionOutcome.Failed, null, null, NoParts);
            }

            return Convert(row);
        }

        public IEnumerable<CharacterResult> ConvertModel(int model, IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            var rows = _tables.CharactersOn(model);

            if (rows.Count == 0)
            {
                Log.Warning($"no characterinfo row uses model {model}");
            }

            return Run(rows, progress, cancellation);
        }

        public IEnumerable<CharacterResult> ConvertAll(IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            return Run(_tables.Characters?.ToList() ?? new List<CharacterInfoRecord>(),
                progress, cancellation);
        }

        private IEnumerable<CharacterResult> Run(IReadOnlyList<CharacterInfoRecord> rows,
            IProgress<ConversionProgress> progress, CancellationToken cancellation)
        {
            return Batch.Run(rows, row => $"character {row.Id}",
                Convert,
                row => new CharacterResult(row.Id, Named(row.Id), ConversionOutcome.Failed,
                    null, null, NoParts),
                progress, cancellation);
        }

        private CharacterResult Convert(CharacterInfoRecord row)
        {
            Log.Info($"converting character {row.Id} '{row.Name}'");

            return row.ModalType switch
            {
                CharacterModalType.MainCharacter => ConvertPlayer(row),
                CharacterModalType.Other => ConvertMonster(row),
                _ => new CharacterResult(row.Id, Named(row.Id), ConversionOutcome.Skipped, null, null, NoParts)
            };
        }

        private CharacterResult ConvertPlayer(CharacterInfoRecord row)
        {
            var name = Named(row.Id);
            var rig = _rigs.Convert(row.Model);

            if (rig == null)
            {
                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            var parts = new List<ItemResult>();

            foreach (var itemId in row.Parts.Take(EquipmentSlots).Where(part => part != 0))
            {
                if (_tables.Items == null || !_tables.Items.TryGetById(itemId, out var item))
                {
                    Log.Warning($"characterinfo {row.Id}: unknown item {itemId}");

                    continue;
                }

                parts.Add(_items.ConvertModules(item, row.Model));
            }

            if (!parts.SelectMany(part => part.Artifacts).Any())
            {
                Log.Error($"no part of character {row.Id} loaded");

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, rig, null, parts);
            }

            var outcome = rig.Outcome == ConversionOutcome.Converted ||
                          parts.Any(part => part.Outcome == ConversionOutcome.Converted)
                ? ConversionOutcome.Converted
                : ConversionOutcome.Skipped;

            return new CharacterResult(row.Id, name, outcome, rig, null, parts);
        }

        private CharacterResult ConvertMonster(CharacterInfoRecord row)
        {
            var name = Named(row.Model);
            var labPath = _settings.Source.Skeleton(row.Model);

            if (_merged.TryGet(labPath, out var already))
            {
                return new CharacterResult(row.Id, name, ConversionOutcome.Skipped, null, already, NoParts);
            }

            var glbPath = _settings.Output.Model(ContentKind.Character, name);

            if (!_settings.Overwrite && File.Exists(glbPath))
            {
                return Merged(row, labPath, new ModelArtifact(name, ContentKind.Character, glbPath,
                    Array.Empty<string>(), ConversionOutcome.Skipped));
            }

            if (!File.Exists(labPath))
            {
                Log.Error($"missing '{labPath}'");

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            BoneAnimation skeleton;

            try
            {
                using var stream = File.OpenRead(labPath);
                skeleton = LabFile.Read(stream).Animation;
            }
            catch (ParseException exception)
            {
                Log.Error($"failed to parse '{labPath}'", exception);

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            var siblings = _tables.CharactersOn(row.Model)
                .Where(record => record.ModalType == CharacterModalType.Other)
                .ToList();
            var parts = LoadParts(siblings[0], row.Model);

            if (parts.Count == 0)
            {
                Log.Error($"no part of model {row.Model} loaded");

                return new CharacterResult(row.Id, name, ConversionOutcome.Failed, null, null, NoParts);
            }

            var packaging = new ModelPackaging(glbPath, _settings.Output.TextureDir(ContentKind.Character));
            var file = GltfExport.Character(name, skeleton, parts.ToArray(),
                _tables.ActionsFor(siblings, row.Model), packaging.TextureUriPrefix);

            var packaged = packaging.Write(file, parts,
                _settings.Source.TextureDir(ContentKind.Character));

            return Merged(row, labPath, new ModelArtifact(name, ContentKind.Character, packaged.ModelPath,
                packaged.TexturePaths, ConversionOutcome.Converted));
        }

        private List<GeometryObject> LoadParts(CharacterInfoRecord row, int model)
        {
            var parts = new List<GeometryObject>();

            for (var slot = 0; slot < row.Parts.Length; slot++)
            {
                if (row.Parts[slot] == 0)
                {
                    continue;
                }

                var fileId = model * 1000000L + row.SuitId * 10000L + slot;
                var partPath = _settings.Source.Model("character", $"{fileId:D10}.lgo");

                if (!File.Exists(partPath))
                {
                    Log.Warning($"missing part '{partPath}'");

                    continue;
                }

                try
                {
                    using var stream = File.OpenRead(partPath);
                    var part = LgoFile.Read(stream).Object;

                    part.Id = (uint)slot;
                    parts.Add(part);
                }
                catch (ParseException exception)
                {
                    Log.Warning($"failed to parse '{partPath}'", exception);
                }
            }

            return parts;
        }

        private CharacterResult Merged(CharacterInfoRecord row, string labPath, ModelArtifact artifact)
        {
            _merged.Add(labPath, artifact);

            return new CharacterResult(row.Id, artifact.Name, artifact.Outcome, null, artifact, NoParts);
        }

        private static string Named(int id) => $"{id:D4}";
    }
}
