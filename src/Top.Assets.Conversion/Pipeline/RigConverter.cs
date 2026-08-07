using System;
using System.IO;
using Top.Assets.Conversion.Models;
using Top.Assets.Conversion.Models.Gltf;
using Top.Logging;
using Top.MindPower;
using Top.MindPower.Animation;
using Top.Tables.Custom;

namespace Top.Assets.Conversion.Pipeline
{
    /// <summary>
    /// Converts a .lab skeleton to a packaged glTF rig, attaching the clips cut
    /// out for every characterinfo row that names the same model. A source path
    /// already converted returns the same artifact marked skipped.
    /// </summary>
    public class RigConverter
    {
        private readonly ConversionSettings _settings;
        private readonly ClientTables _tables;

        private readonly ArtifactMemo _memo = new ArtifactMemo();

        public RigConverter(ConversionSettings settings, ClientTables tables)
        {
            _settings = settings;
            _tables = tables;
        }

        public ModelArtifact Convert(int model)
        {
            return Convert(_settings.Source.Skeleton(model));
        }

        public ModelArtifact Convert(string labPath)
        {
            if (_memo.TryGet(labPath, out var already))
            {
                return already;
            }

            var name = Path.GetFileNameWithoutExtension(labPath);
            var rigDir = _settings.Output.RigDir(name);
            var glbPath = _settings.Output.Rig(name);

            if (!_settings.Overwrite && File.Exists(glbPath))
            {
                return _memo.Add(labPath, new ModelArtifact(name, ContentKind.Character, glbPath, Array.Empty<string>(),
                    ConversionOutcome.Skipped));
            }

            if (!File.Exists(labPath))
            {
                Log.Error($"missing '{labPath}'");

                return null;
            }

            Log.Info($"converting rig '{name}'");

            BoneAnimation skeleton;

            try
            {
                using var stream = File.OpenRead(labPath);
                skeleton = LabFile.Read(stream).Animation;
            }
            catch (ParseException exception)
            {
                Log.Error($"failed to parse '{labPath}'", exception);

                return null;
            }

            var file = GltfExport.Rig(name, skeleton, LoadActions(name));
            var packaged = new ModelPackaging(rigDir).Write(file, name);

            return _memo.Add(labPath, new ModelArtifact(name, ContentKind.Character, packaged.ModelPath,
                packaged.TexturePaths, ConversionOutcome.Converted));
        }

        private CharacterAction[] LoadActions(string name)
        {
            if (!int.TryParse(name, out var model))
            {
                Log.Warning($"'{name}' is not a skeleton model id");

                return null;
            }

            var rows = _tables.CharactersOn(model);

            if (rows.Count == 0)
            {
                Log.Warning($"no characterinfo row uses model {model}");

                return null;
            }

            return _tables.ActionsFor(rows, model);
        }
    }
}
