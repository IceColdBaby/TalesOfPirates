using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Assets.Conversion.Textures;
using Top.Gltf;
using Top.Logging;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Models
{
    /// <summary>
    /// What a built model became on disk.
    /// </summary>
    public class PackagedModel
    {
        public PackagedModel(string modelPath, string binPath, IReadOnlyList<string> texturePaths)
        {
            ModelPath = modelPath;
            BinPath = binPath;
            TexturePaths = texturePaths;
        }

        public string ModelPath { get; }

        public string BinPath { get; }

        public IReadOnlyList<string> TexturePaths { get; }
    }

    /// <summary>
    /// Writes a built model into the converted tree: the glTF itself, and the
    /// PNGs its source objects name.
    /// </summary>
    public class ModelPackaging
    {
        private readonly string _modelOutputDir;
        private readonly string _textureOutputDir;
        private readonly GltfPackaging _packaging;

        public ModelPackaging(string modelOutputDir, string textureOutputDir = null,
            GltfPackaging packaging = GltfPackaging.Glb)
        {
            if (!Enum.IsDefined(typeof(GltfPackaging), packaging))
            {
                throw new ArgumentOutOfRangeException(nameof(packaging), packaging, null);
            }

            _modelOutputDir = modelOutputDir;
            _textureOutputDir = textureOutputDir;
            _packaging = packaging;
        }

        public string TextureUriPrefix => Path
            .GetRelativePath(_modelOutputDir, _textureOutputDir)
            .Replace('\\', '/');

        public PackagedModel Write(GltfFile file, string name)
        {
            Directory.CreateDirectory(_modelOutputDir);

            var modelPath = WriteModel(file, name, out var binPath);

            return new PackagedModel(modelPath, binPath, Array.Empty<string>());
        }

        /// <summary>
        /// Writes a model together with the PNGs its source objects name,
        /// converting from the original client texture formats on the way.
        /// </summary>
        public PackagedModel Write(GltfFile file, IReadOnlyList<GeometryObject> objects, string name,
            string textureSearchDir)
        {
            Directory.CreateDirectory(_modelOutputDir);
            Directory.CreateDirectory(_textureOutputDir);

            var modelPath = WriteModel(file, name, out var binPath);
            var texturePaths = WriteTextures(objects, textureSearchDir);

            return new PackagedModel(modelPath, binPath, texturePaths);
        }

        private static IEnumerable<TextureStage> CollectTextureStages(IReadOnlyList<GeometryObject> objects)
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
                            Log.Warning($"multi-stage texture '{stage.FileName}' (stage {s}) ignored");
                            continue;
                        }

                        if (seen.TryAdd(stage.FileName, stage))
                        {
                            yield return stage;
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
                            Log.Warning($"texture-image animation on stage {stage} ignored");
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
                                yield return frame;
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

        private string WriteModel(GltfFile file, string name, out string binPath)
        {
            binPath = null;

            switch (_packaging)
            {
                case GltfPackaging.GltfEmbedded:
                    {
                        var modelPath = Path.Combine(_modelOutputDir, name + ".gltf");

                        using var fs = File.Create(modelPath);
                        GltfWriter.WriteGltfEmbedded(file.Document, file.BinChunk, fs);

                        return modelPath;
                    }

                case GltfPackaging.GltfWithBin:
                    {
                        var modelPath = Path.Combine(_modelOutputDir, name + ".gltf");
                        binPath = Path.Combine(_modelOutputDir, name + ".bin");

                        using var json = File.Create(modelPath);
                        using var bin = File.Create(binPath);
                        GltfWriter.WriteGltfWithBin(file.Document, file.BinChunk, name + ".bin", json, bin);

                        return modelPath;
                    }

                case GltfPackaging.Glb:
                    {
                        var modelPath = Path.Combine(_modelOutputDir, name + ".glb");

                        using var fs = File.Create(modelPath);
                        GltfWriter.WriteGlb(file.Document, file.BinChunk, fs);

                        return modelPath;
                    }

                default:
                    throw new InvalidOperationException($"unhandled packaging {_packaging}");
            }
        }

        private List<string> WriteTextures(IReadOnlyList<GeometryObject> objects, string searchDir)
        {
            var written = new List<string>();

            foreach (var stage in CollectTextureStages(objects))
            {
                var fileName = stage.FileName;
                var pngPath = Path.Combine(_textureOutputDir, Path.GetFileNameWithoutExtension(fileName) + ".png");

                if (File.Exists(pngPath))
                {
                    written.Add(pngPath);
                    continue;
                }

                var sourcePath = ResolveTexture(searchDir, fileName);

                if (sourcePath == null)
                {
                    Log.Warning($"texture '{fileName}' not found in '{searchDir}'");
                    continue;
                }

                byte[] png;

                try
                {
                    png = TextureConversion.ToPng(File.ReadAllBytes(sourcePath), stage);
                }
                catch (InvalidDataException exception)
                {
                    Log.Warning($"texture '{fileName}': {exception.Message}");
                    continue;
                }

                File.WriteAllBytes(pngPath, png);

                written.Add(pngPath);
            }

            return written;
        }
    }
}
