using System;
using System.Collections.Generic;
using System.IO;
using Top.Assets.Conversion.Materials;
using Top.Gltf;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor
{
    /// <summary>
    /// Remaps a glTF file's materials onto the importer by material name.
    /// A project material with a matching name is reused, so authored
    /// render state survives DCC round trips; otherwise a Top/Legacy seed
    /// is created from the glTF material section. Existing material assets
    /// are never overwritten. Embedded images are extracted next to the
    /// model under Textures/.
    /// </summary>
    public static class GltfMaterialExtractor
    {
        public static List<string> Extract(string glbAssetPath, string materialsDir,
            bool reuseProjectMaterials = true)
        {
            var warnings = new List<string>();
            GltfFile file;
            using (var stream = File.OpenRead(glbAssetPath))
            {
                file = GltfReader.Read(stream);
            }

            var doc = file.Document;
            if (doc.Materials == null || doc.Materials.Count == 0)
            {
                warnings.Add($"'{glbAssetPath}' defines no materials");
                return warnings;
            }

            var glbDir = Path.GetDirectoryName(glbAssetPath).Replace('\\', '/');
            var data = new GltfData(file,
                uri => File.ReadAllBytes(Path.Combine(glbDir, uri)));
            var importer = AssetImporter.GetAtPath(glbAssetPath);
            var textureByImage = new Dictionary<int, Texture2D>();

            for (var i = 0; i < doc.Materials.Count; i++)
            {
                var gltfMaterial = doc.Materials[i];
                var name = gltfMaterial.Name ?? $"material_{i}";
                var assetPath = $"{materialsDir}/{SanitizeFileName(name)}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (material == null && reuseProjectMaterials)
                {
                    material = FindProjectMaterial(name, warnings);
                }

                if (material == null)
                {
                    var spec = RenderStateMapper.ToSpec(GltfMaterialMapper.Map(gltfMaterial));
                    foreach (var warning in spec.Warnings)
                    {
                        warnings.Add($"{name}: {warning}");
                    }

                    var texture = ResolveTexture(doc, data, gltfMaterial, glbAssetPath, textureByImage, warnings);
                    material = RenderStateMapper.CreateMaterial(spec, texture);
                    material.name = Path.GetFileNameWithoutExtension(assetPath);
                    Directory.CreateDirectory(materialsDir);
                    AssetDatabase.CreateAsset(material, assetPath);
                }

                importer.AddRemap(
                    new AssetImporter.SourceAssetIdentifier(typeof(Material), name),
                    material);
            }

            AssetDatabase.WriteImportSettingsIfDirty(glbAssetPath);
            AssetDatabase.ImportAsset(glbAssetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            return warnings;
        }

        private static Material FindProjectMaterial(string name, List<string> warnings)
        {
            var fileName = SanitizeFileName(name);
            var matches = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets($"t:Material {fileName}"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == fileName)
                {
                    matches.Add(path);
                }
            }

            if (matches.Count == 0)
            {
                return null;
            }

            if (matches.Count > 1)
            {
                warnings.Add(
                    $"{name}: multiple project materials share the name; using '{matches[0]}'");
            }

            return AssetDatabase.LoadAssetAtPath<Material>(matches[0]);
        }

        private static Texture2D ResolveTexture(GltfDocument doc, GltfData data,
            GltfMaterial material, string glbAssetPath,
            Dictionary<int, Texture2D> textureByImage, List<string> warnings)
        {
            var info = material.PbrMetallicRoughness?.BaseColorTexture;
            if (info == null)
            {
                return null;
            }

            if (doc.Textures == null || info.Index >= doc.Textures.Count
                || doc.Textures[info.Index].Source == null)
            {
                warnings.Add($"texture {info.Index} has no image source");
                return null;
            }

            var imageIndex = doc.Textures[info.Index].Source.Value;
            if (textureByImage.TryGetValue(imageIndex, out var cached))
            {
                return cached;
            }

            var image = doc.Images[imageIndex];
            string assetPath;
            if (image.BufferView != null)
            {
                assetPath = ExtractEmbeddedImage(data, image, imageIndex, glbAssetPath);
            }
            else if (!string.IsNullOrEmpty(image.Uri)
                && !image.Uri.StartsWith("data:", StringComparison.Ordinal))
            {
                var glbDir = Path.GetDirectoryName(Path.GetFullPath(glbAssetPath));
                assetPath = ToAssetPath(
                    Path.Combine(glbDir, Uri.UnescapeDataString(image.Uri)));
            }
            else
            {
                warnings.Add($"image {imageIndex} has an unsupported source");
                textureByImage[imageIndex] = null;
                return null;
            }

            var texture = assetPath != null
                ? AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath)
                : null;
            if (texture == null)
            {
                warnings.Add($"image {imageIndex} did not resolve to a texture asset");
            }

            textureByImage[imageIndex] = texture;
            return texture;
        }

        private static string ExtractEmbeddedImage(
            GltfData data, GltfImage image, int imageIndex, string glbAssetPath)
        {
            var extension = image.MimeType == "image/jpeg" ? ".jpg" : ".png";
            var name = image.Name != null
                ? SanitizeFileName(image.Name)
                : $"{Path.GetFileNameWithoutExtension(glbAssetPath)}_image_{imageIndex}";
            var directory = $"{Path.GetDirectoryName(glbAssetPath).Replace('\\', '/')}/Textures";
            var assetPath = $"{directory}/{name}{extension}";
            if (!File.Exists(assetPath))
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(assetPath, data.ReadBufferView(image.BufferView.Value));
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                if (AssetImporter.GetAtPath(assetPath) is TextureImporter textureImporter
                    && !textureImporter.alphaIsTransparency)
                {
                    textureImporter.alphaIsTransparency = true;
                    textureImporter.SaveAndReimport();
                }
            }

            return assetPath;
        }

        private static string ToAssetPath(string fullPath)
        {
            var projectRoot = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
            var full = Path.GetFullPath(fullPath).Replace('\\', '/');
            return full.StartsWith(projectRoot + "/", StringComparison.Ordinal)
                ? full.Substring(projectRoot.Length + 1)
                : null;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return name;
        }
    }
}
