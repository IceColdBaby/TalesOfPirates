using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Assets.Conversion.Textures;
using Top.Gltf;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;
using Top.Tables.Custom;

namespace Top.Assets.Conversion
{
    public class ConversionResult
    {
        public string ModelPath;
        public string BinPath;
        public List<string> TexturePngPaths = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// Full engine-agnostic conversion pass: glTF plus PNG textures on disk.
    /// Existing PNGs are kept (shared texture pool); the glTF is always rewritten.
    /// </summary>
    public static class ModelConversion
    {
        public static ConversionResult Convert(SceneModel model, string name,
            string textureSearchDir, string modelOutputDir, string textureOutputDir,
            GltfPackaging packaging = GltfPackaging.Glb)
        {
            var conversion = GeometryObjectGltfMapper.Map(model, name,
                TextureUriPrefix(modelOutputDir, textureOutputDir));

            return Finish(conversion, model.GeometryObjects, name,
                textureSearchDir, modelOutputDir, textureOutputDir, packaging);
        }

        public static ConversionResult Convert(GeometryObject obj, string name,
            string textureSearchDir, string modelOutputDir, string textureOutputDir,
            GltfPackaging packaging = GltfPackaging.Glb, int? litSubset = null)
        {
            var conversion = GeometryObjectGltfMapper.Map(obj, name,
                TextureUriPrefix(modelOutputDir, textureOutputDir), litSubset);

            return Finish(conversion, new[] { obj }, name,
                textureSearchDir, modelOutputDir, textureOutputDir, packaging);
        }

        public static ConversionResult Convert(GeometryObject obj, BoneAnimation skeleton,
            string name, string textureSearchDir, string modelOutputDir, string textureOutputDir,
            GltfPackaging packaging = GltfPackaging.Glb)
        {
            var conversion = GeometryObjectGltfMapper.Map(obj, skeleton, name,
                TextureUriPrefix(modelOutputDir, textureOutputDir));

            return Finish(conversion, new[] { obj }, name,
                textureSearchDir, modelOutputDir, textureOutputDir, packaging);
        }

        public static ConversionResult ConvertRig(BoneAnimation skeleton,
            CharacterAction[] actions, string name, string modelOutputDir,
            GltfPackaging packaging = GltfPackaging.Glb)
        {
            var conversion = RigGltfMapper.Map(skeleton, actions, name);

            Directory.CreateDirectory(modelOutputDir);

            var result = new ConversionResult();
            result.Warnings.AddRange(conversion.Warnings);

            WriteModel(conversion, packaging, modelOutputDir, name, result);

            return result;
        }

        public static ConversionResult ConvertCharacter(BoneAnimation skeleton,
            GeometryObject[] parts, CharacterAction[] actions, string name,
            string textureSearchDir, string modelOutputDir, string textureOutputDir,
            GltfPackaging packaging = GltfPackaging.Glb, bool emitClips = true)
        {
            var conversion = CharacterGltfMapper.Map(skeleton, parts, actions, name,
                TextureUriPrefix(modelOutputDir, textureOutputDir), emitClips);

            return Finish(conversion, parts, name,
                textureSearchDir, modelOutputDir, textureOutputDir, packaging);
        }

        private static string TextureUriPrefix(string modelOutputDir, string textureOutputDir)
        {
            return Path.GetRelativePath(modelOutputDir, textureOutputDir).Replace('\\', '/');
        }

        private static ConversionResult Finish(GltfConversion conversion,
            IReadOnlyList<GeometryObject> objects, string name, string textureSearchDir,
            string modelOutputDir, string textureOutputDir, GltfPackaging packaging)
        {
            Directory.CreateDirectory(modelOutputDir);
            Directory.CreateDirectory(textureOutputDir);

            var result = new ConversionResult();
            result.Warnings.AddRange(conversion.Warnings);

            WriteModel(conversion, packaging, modelOutputDir, name, result);

            foreach ((string fileName, TextureStage stage) in CollectTextureStages(objects, result.Warnings))
            {
                var pngPath = Path.Combine(textureOutputDir, Path.GetFileNameWithoutExtension(fileName) + ".png");

                if (File.Exists(pngPath))
                {
                    result.TexturePngPaths.Add(pngPath);
                    continue;
                }

                var sourcePath = ResolveTexture(textureSearchDir, fileName);

                if (sourcePath == null)
                {
                    result.Warnings.Add($"texture '{fileName}' not found in '{textureSearchDir}'");
                    continue;
                }

                byte[] png;

                try
                {
                    png = TextureConversion.ToPng(File.ReadAllBytes(sourcePath), stage);
                }
                catch (InvalidDataException exception)
                {
                    result.Warnings.Add($"texture '{fileName}': {exception.Message}");
                    continue;
                }

                File.WriteAllBytes(pngPath, png);

                result.TexturePngPaths.Add(pngPath);
            }

            return result;
        }

        private static void WriteModel(GltfConversion conversion, GltfPackaging packaging,
            string outputDir, string name, ConversionResult result)
        {
            switch (packaging)
            {
                case GltfPackaging.GltfEmbedded:
                    {
                        result.ModelPath = Path.Combine(outputDir, name + ".gltf");

                        using var fs = File.Create(result.ModelPath);
                        GltfWriter.WriteGltfEmbedded(conversion.Document, conversion.Bin, fs);

                        break;
                    }

                case GltfPackaging.GltfWithBin:
                    {
                        result.ModelPath = Path.Combine(outputDir, name + ".gltf");
                        result.BinPath = Path.Combine(outputDir, name + ".bin");

                        using var json = File.Create(result.ModelPath);
                        using var bin = File.Create(result.BinPath);
                        GltfWriter.WriteGltfWithBin(conversion.Document, conversion.Bin, name + ".bin", json, bin);

                        break;
                    }
                case GltfPackaging.Glb:
                    {
                        result.ModelPath = Path.Combine(outputDir, name + ".glb");

                        using var fs = File.Create(result.ModelPath);
                        GltfWriter.WriteGlb(conversion.Document, conversion.Bin, fs);

                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(packaging), packaging, null);
            }
        }

        private static IEnumerable<(string FileName, TextureStage Stage)>
            CollectTextureStages(IReadOnlyList<GeometryObject> objects, List<string> warnings)
        {
            var seen = new Dictionary<string, TextureStage>();

            foreach (var obj in objects)
            {
                foreach (var material in obj.Materials ?? Enumerable.Empty<MaterialTexture>())
                {
                    if (material.Stages == null)
                    {
                        continue;
                    }

                    for (var s = 0; s < material.Stages.Length; s++)
                    {
                        var stage = material.Stages[s];

                        if (stage == null || string.IsNullOrEmpty(stage.FileName))
                        {
                            continue;
                        }

                        if (s > 0)
                        {
                            warnings.Add($"multi-stage texture '{stage.FileName}' (stage {s}) ignored");
                            continue;
                        }

                        if (seen.TryAdd(stage.FileName, stage))
                        {
                            yield return (stage.FileName, stage);
                        }
                    }
                }

                var flipbooks = obj.Animation?.TextureImage;

                if (flipbooks == null)
                {
                    continue;
                }

                for (var subset = 0; subset < flipbooks.GetLength(0); subset++)
                {
                    for (var stage = 0; stage < flipbooks.GetLength(1); stage++)
                    {
                        var track = flipbooks[subset, stage];

                        if (track?.DataSequence == null || track.DataSequence.Length == 0)
                        {
                            continue;
                        }

                        if (stage > 0)
                        {
                            warnings.Add($"texture-image animation on stage {stage} ignored");
                            continue;
                        }

                        foreach (var frame in track.DataSequence)
                        {
                            if (frame == null || string.IsNullOrEmpty(frame.FileName))
                            {
                                continue;
                            }

                            if (seen.TryAdd(frame.FileName, frame))
                            {
                                yield return (frame.FileName, frame);
                            }
                        }
                    }
                }
            }
        }

        private static string ResolveTexture(string searchDir, string fileName)
        {
            return new[]
                {
                    Path.ChangeExtension(fileName, ".dds"),
                    fileName,
                    Path.ChangeExtension(fileName, ".tga"),
                }
                .Select(candidate => Path.Combine(searchDir, candidate))
                .FirstOrDefault(File.Exists);
        }
    }
}
