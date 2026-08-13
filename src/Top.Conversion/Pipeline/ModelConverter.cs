using System;
using System.IO;
using Top.Conversion.Models;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Logging;
using Top.Legacy.MindPower;
using Top.Legacy.MindPower.Animation;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// Converts a client model (.lmo scene or .lgo object) to a packaged glTF
    /// under the output kind folder. Skinned .lgo objects pick up the matching
    /// .lab skeleton if one is beside them, otherwise convert rigid. A source
    /// path already converted returns the same artifact marked skipped.
    /// </summary>
    public class ModelConverter
    {
        private readonly ConversionSettings _settings;

        private readonly ArtifactMemo _memo = new ArtifactMemo();

        public ModelConverter(ConversionSettings settings)
        {
            _settings = settings;
        }

        public ModelArtifact Convert(string modelPath)
        {
            if (_memo.TryGet(modelPath, out var already))
            {
                return already;
            }

            var kind = DetectKind(modelPath);

            if (kind == null)
            {
                Log.Error($"cannot derive model kind from '{modelPath}': expected a path under <...>/model/<kind>/");

                return null;
            }

            var name = Path.GetFileNameWithoutExtension(modelPath).ToLowerInvariant();
            var glbPath = _settings.Output.Model(kind, name);

            if (!_settings.Overwrite && File.Exists(glbPath))
            {
                return _memo.Add(modelPath,
                    new ModelArtifact(name, kind, glbPath, Array.Empty<string>(), ConversionOutcome.Skipped));
            }

            if (!File.Exists(modelPath))
            {
                Log.Error($"missing '{modelPath}'");

                return null;
            }

            Log.Info($"converting {kind} model '{name}'");

            var isObject = modelPath.EndsWith(".lgo", StringComparison.OrdinalIgnoreCase);

            SceneModel model = null;
            GeometryObject obj = null;

            try
            {
                using var stream = File.OpenRead(modelPath);

                if (isObject)
                {
                    obj = LgoFile.Read(stream).Object;
                }
                else
                {
                    model = LmoFile.Read(stream).Model;
                }
            }
            catch (ParseException exception)
            {
                Log.Error($"failed to parse '{modelPath}'", exception);

                return null;
            }

            var packaging = new ModelPackaging(glbPath, _settings.Output.TextureDir(kind));
            var uriPrefix = packaging.TextureUriPrefix;
            var objects = isObject ? new[] { obj } : model.GeometryObjects;
            var file = isObject
                ? BuildObject(obj, name, kind, uriPrefix)
                : GltfExport.Model(name, model, uriPrefix);

            var packaged = packaging.Write(file, objects, _settings.Source.TextureDir(kind));

            return _memo.Add(modelPath, new ModelArtifact(name, kind, packaged.ModelPath,
                packaged.TexturePaths, ConversionOutcome.Converted));
        }

        private GltfFile BuildObject(GeometryObject obj, string name, string kind, string uriPrefix)
        {
            var skinned = obj.Mesh?.SkinBlends != null && obj.Mesh.SkinBlends.Length > 0;
            var skeleton = skinned ? LoadSkeleton(name) : null;

            return skeleton != null
                ? GltfExport.Object(name, obj, skeleton, uriPrefix)
                : GltfExport.Object(name, obj, uriPrefix, kind == ContentKind.Item ? 1 : null);
        }

        private BoneAnimation LoadSkeleton(string modelName)
        {
            if (modelName.Length < 4)
            {
                return null;
            }

            var labPath = _settings.Source.Skeleton(modelName.Substring(0, 4));

            if (!File.Exists(labPath))
            {
                Log.Warning($"no skeleton at '{labPath}', converting rigid");

                return null;
            }

            try
            {
                using var stream = File.OpenRead(labPath);

                return LabFile.Read(stream).Animation;
            }
            catch (ParseException exception)
            {
                Log.Warning($"failed to parse '{labPath}': {exception.Message}");

                return null;
            }
        }

        private static string DetectKind(string modelPath)
        {
            for (var dir = Path.GetDirectoryName(modelPath);
                 !string.IsNullOrEmpty(dir);
                 dir = Path.GetDirectoryName(dir))
            {
                var parent = Path.GetDirectoryName(dir);

                if (!string.IsNullOrEmpty(parent) && string.Equals(Path.GetFileName(parent), "model",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFileName(dir).ToLowerInvariant();
                }
            }

            return null;
        }
    }
}
