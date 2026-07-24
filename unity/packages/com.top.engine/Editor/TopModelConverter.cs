using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Assets.Conversion;
using Top.Assets.Conversion.Materials;
using Top.MindPower.Animation;
using Top.MindPower.Geometry;
using Top.Tables;
using Top.Tables.Custom;
using Top.Tables.Records;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor
{
    /// <summary>
    /// Single-pass in-editor conversion: .lmo/.lgo to GLB + PNGs + scaffolded
    /// materials and prefab, laid out per model kind (scene, item, character)
    /// derived from the source path under the client model root (configured
    /// in Top/Importer). Materials are bound to the imported model via the
    /// importer's external object map. Composed human characters are
    /// prefabs referencing shared part assets; other kinds scaffold a
    /// prefab variant of the imported model carrying only the game
    /// components. Scaffold-once: materials and prefab are never overwritten
    /// unless the re-scaffold entry point is used. Rigs (.lab) convert to
    /// bare GLBs carrying the skeleton and per-action clips, with nothing
    /// scaffolded.
    /// </summary>
    public static class TopModelConverter
    {
        private const string ContentRigRoot = "Assets/Content/Character/Rigs";
        private const string ContentRoot = "Assets/Content";
        private const string ContentTextureRoot = "Assets/Content/Textures";

        public static void ConvertRig(string labPath)
        {
            if (!ImporterSettings.RequireClientRoot())
            {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(labPath);
            BoneAnimation skeleton;

            try
            {
                using (var stream = File.OpenRead(labPath))
                {
                    skeleton = LabFile.Read(stream).Animation;
                }
            }
            catch (MindPower.ParseException exception)
            {
                Debug.LogError($"[Top] failed to parse '{labPath}': {exception.Message}");
                return;
            }

            CharacterAction[] actions = null;

            if (!int.TryParse(name, out var model))
            {
                Debug.LogWarning($"[Top] '{name}' is not a skeleton model id");
            }
            else
            {
                var rows = LoadCharacterRows(model);

                if (rows != null)
                {
                    if (rows.Count == 0)
                    {
                        Debug.LogWarning($"[Top] no characterinfo row uses model {model}");
                    }
                    else
                    {
                        actions = LoadActions(ResolveActionId(rows, model));
                    }
                }
            }

            var modelDir = $"{ContentRigRoot}/{name}";
            var result = ModelConversion.ConvertRig(skeleton, actions, name,
                Path.GetFullPath(modelDir));
            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"[Top] {warning}");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath($"{modelDir}/{name}.glb");
            Debug.Log($"[Top] converted rig {modelDir}/{name}.glb");
        }

        public static void ConvertCharacter(string labPath, bool overwrite)
        {
            if (!ImporterSettings.RequireClientRoot())
            {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(labPath);

            if (!int.TryParse(name, out var model))
            {
                Debug.LogError($"[Top] '{name}' is not a skeleton model id");
                return;
            }

            BoneAnimation skeleton;

            try
            {
                using (var stream = File.OpenRead(labPath))
                {
                    skeleton = LabFile.Read(stream).Animation;
                }
            }
            catch (MindPower.ParseException exception)
            {
                Debug.LogError($"[Top] failed to parse '{labPath}': {exception.Message}");
                return;
            }

            var all = LoadCharacterRows(model);

            if (all == null)
            {
                return;
            }

            var monsterRows = all
                .Where(record => record.ModalType == CharacterModalType.Other)
                .ToList();
            var humanRows = all
                .Where(record => record.ModalType == CharacterModalType.MainCharacter)
                .ToList();

            if (monsterRows.Count == 0 && humanRows.Count == 0)
            {
                Debug.LogWarning($"[Top] no convertible characterinfo row uses model {model}");
                return;
            }

            if (humanRows.Count > 0)
            {
                EnsureRig(labPath, model);

                // The rig's clips were baked from the first row's action
                // set; a human row that disagrees plays the wrong timeline.
                var rigActionId = all[0].ActionId;

                foreach (var other in humanRows.Where(record => record.ActionId != rigActionId))
                {
                    Debug.LogWarning($"[Top] characterinfo {other.Id} '{other.Name}' plays " +
                        $"action set {other.ActionId}; rig clips use set {rigActionId}");
                }

                var items = LoadItemInfo();

                if (items != null)
                {
                    foreach (var row in humanRows)
                    {
                        try
                        {
                            ConvertPlayerCharacter(row, items, overwrite);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError($"[Top] characterinfo {row.Id} failed: {exception}");
                        }
                    }
                }
            }

            if (monsterRows.Count > 0)
            {
                ConvertMonsterCharacter(labPath, skeleton, monsterRows, model, overwrite);
            }
        }

        public static void ConvertCharacterModel(int model, bool overwrite)
        {
            if (!ImporterSettings.RequireClientRoot())
            {
                return;
            }

            var labPath = Path.Combine(ImporterSettings.instance.AnimationRoot, $"{model:D4}.lab");

            if (!File.Exists(labPath))
            {
                Debug.LogError($"[Top] no skeleton at '{labPath}'");
                return;
            }

            ConvertCharacter(labPath, overwrite);
        }

        public static void ConvertSceneObject(int id, bool overwrite)
        {
            if (!ImporterSettings.RequireClientRoot())
            {
                return;
            }

            var table = LoadSceneObjectInfo();

            if (table == null)
            {
                return;
            }

            if (!table.TryGetById(id, out var row) || string.IsNullOrEmpty(row.Name))
            {
                Debug.LogError($"[Top] no sceneobjinfo row {id} with a model file");
                return;
            }

            var source = Path.Combine(ImporterSettings.instance.ModelRoot, "scene", row.Name);

            if (!File.Exists(source))
            {
                Debug.LogError($"[Top] missing '{source}'");
                return;
            }

            Convert(source, overwrite);
        }

        private static void EnsureRig(string labPath, int model)
        {
            var rigPath = $"{ContentRigRoot}/{model:D4}/{model:D4}.glb";

            if (AssetDatabase.LoadMainAssetAtPath(rigPath) == null)
            {
                ConvertRig(labPath);
            }
        }

        private static void ConvertMonsterCharacter(string labPath, BoneAnimation skeleton,
            List<CharacterInfoRecord> rows, int model, bool overwrite)
        {
            var name = Path.GetFileNameWithoutExtension(labPath);
            var row = rows[0];
            var actions = LoadActions(ResolveActionId(rows, model));
            var parts = new List<GeometryObject>();

            for (var i = 0; i < row.Parts.Length; i++)
            {
                if (row.Parts[i] == 0)
                {
                    continue;
                }

                var fileId = model * 1000000L + row.SuitId * 10000L + i;
                var partPath = Path.Combine(
                    ImporterSettings.instance.ModelRoot, "character", $"{fileId:D10}.lgo");

                if (!File.Exists(partPath))
                {
                    Debug.LogWarning($"[Top] missing part '{partPath}'");
                    continue;
                }

                try
                {
                    using var stream = File.OpenRead(partPath);
                    var part = LgoFile.Read(stream).Object;

                    // Slot indices are unique per character while .lgo
                    // object ids are not, and every downstream name -
                    // nodes, materials, prefab lookups - keys on the id.
                    part.Id = (uint)i;
                    parts.Add(part);
                }
                catch (MindPower.ParseException exception)
                {
                    Debug.LogWarning($"[Top] failed to parse '{partPath}': {exception.Message}");
                }
            }

            if (parts.Count == 0)
            {
                Debug.LogError($"[Top] no part of model {model} loaded");
                return;
            }

            var modelDir = $"{ContentRoot}/Character/Models/{name}";
            var prefabPath = $"{ContentRoot}/Character/Prefabs/{name}.prefab";

            if (!overwrite && File.Exists(prefabPath))
            {
                Debug.LogWarning($"[Top] {prefabPath} exists; skipped (enable overwrite to replace)");
                return;
            }

            var textureDir = $"{ContentTextureRoot}/Character";
            var textureSearchDir = Path.Combine(ImporterSettings.instance.TextureRoot, "character");

            var result = ModelConversion.ConvertCharacter(skeleton, parts.ToArray(), actions,
                name, textureSearchDir, Path.GetFullPath(modelDir),
                Path.GetFullPath(textureDir));

            ScaffoldModel(labPath, name, "Character", parts.ToArray(), result, modelDir,
                prefabPath, textureDir, overwrite);
        }

        private static GameObject EnsurePart(string module)
        {
            var assetPath = $"{ContentRoot}/Character/Models/{module}/{module}.glb";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (existing != null)
            {
                return existing;
            }

            var partPath = Path.Combine(ImporterSettings.instance.ModelRoot, "character", $"{module}.lgo");

            if (!File.Exists(partPath))
            {
                Debug.LogWarning($"[Top] missing part '{partPath}'");
                return null;
            }

            Convert(partPath, overwrite: true);

            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        private static void ConvertPlayerCharacter(CharacterInfoRecord row,
            Table<ItemInfoRecord> items, bool overwrite)
        {
            var name = $"{row.Id:D4}";
            var prefabPath = $"{ContentRoot}/Character/Prefabs/{name}.prefab";

            if (!overwrite && File.Exists(prefabPath))
            {
                return;
            }

            var rigPath = $"{ContentRigRoot}/{row.Model:D4}/{row.Model:D4}.glb";
            var rig = AssetDatabase.LoadMainAssetAtPath(rigPath) as GameObject;

            if (rig == null)
            {
                Debug.LogError($"[Top] no rig at '{rigPath}'");
                return;
            }

            var parts = new List<SkinnedMeshRenderer>();

            // The client's player path reads exactly five slots
            // (SceneCreateNode.cpp builds part_buf[5]); values in
            // slots 5-7 exist in the table but are never loaded.
            for (var i = 0; i < 5; i++)
            {
                if (row.Parts[i] == 0)
                {
                    continue;
                }

                if (!items.TryGetById(row.Parts[i], out var item))
                {
                    Debug.LogWarning($"[Top] characterinfo {row.Id}: unknown item {row.Parts[i]}");
                    continue;
                }

                if (!ItemModules.TryGetModule(item, row.Model, out var module))
                {
                    Debug.LogWarning($"[Top] item {item.Id} '{item.Name}' has no model " +
                        $"for framework {row.Model}");
                    continue;
                }

                var partModel = EnsurePart(module);
                ItemConversion.EnsureDefinition(item);

                if (partModel == null)
                {
                    continue;
                }

                var source = partModel.GetComponentInChildren<SkinnedMeshRenderer>(true);

                if (source == null)
                {
                    Debug.LogWarning($"[Top] part '{module}' has no skinned mesh");
                    continue;
                }

                parts.Add(source);
            }

            if (parts.Count == 0)
            {
                Debug.LogError($"[Top] no part of character {row.Id} loaded");
                return;
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(rig);

            try
            {
                var bones = new Dictionary<string, Transform>();

                foreach (var node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!bones.TryAdd(node.name, node))
                    {
                        Debug.LogWarning($"[Top] rig has more than one node named " +
                            $"'{node.name}'; parts bind to the first");
                    }
                }

                foreach (var source in parts)
                {
                    SkinnedPartBinder.Bind(source, root.transform, bones);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            var modelDir = $"{ContentRoot}/Character/Models/{name}";

            if (AssetDatabase.IsValidFolder(modelDir))
            {
                AssetDatabase.DeleteAsset(modelDir);
            }

            Debug.Log($"[Top] composed {prefabPath}");
        }

        internal static Table<CharacterInfoRecord> LoadCharacterInfo()
        {
            var infoPath = ImporterSettings.instance.TablePath("characterinfo.txt");

            if (!File.Exists(infoPath))
            {
                Debug.LogWarning($"[Top] missing '{infoPath}'");
                return null;
            }

            using (var stream = File.OpenRead(infoPath))
            {
                return TableFile.Read<CharacterInfoRecord>(stream);
            }
        }

        /// <summary>
        /// All characterinfo rows whose model column matches the skeleton
        /// model id, in file order. A missing table warns and yields null;
        /// an empty list means the table is present but no row matches.
        /// </summary>
        private static List<CharacterInfoRecord> LoadCharacterRows(int model)
        {
            return LoadCharacterInfo()?.Where(record => record.Model == model).ToList();
        }

        internal static Table<ItemInfoRecord> LoadItemInfo()
        {
            var path = ImporterSettings.instance.TablePath("iteminfo.txt");

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Top] missing '{path}'");
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                return TableFile.Read<ItemInfoRecord>(stream);
            }
        }

        /// <summary>
        /// Scene object info table indexed by id column.
        /// </summary>
        internal static Table<SceneObjectInfoRecord> LoadSceneObjectInfo()
        {
            var path = ImporterSettings.instance.TablePath("sceneobjinfo.txt");

            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Top] missing '{path}'");
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                return TableFile.Read<SceneObjectInfoRecord>(stream);
            }
        }

        /// <summary>
        /// The action set belongs to the character, not the skeleton: the
        /// client loads "%04d.lab" from characterinfo's model column but
        /// picks the CharacterAction.tx block by the row's Action ID
        /// column (LoadPose(pInfo->sActionID)). First row wins; rows that
        /// disagree warn.
        /// </summary>
        private static int ResolveActionId(List<CharacterInfoRecord> rows, int model)
        {
            var actionId = rows[0].ActionId;

            foreach (var other in rows.Where(record => record.ActionId != actionId))
            {
                Debug.LogWarning($"[Top] characterinfo {other.Id} '{other.Name}' plays " +
                    $"action set {other.ActionId} on model {model}; keeping {actionId}");
            }

            return actionId;
        }

        private static CharacterAction[] LoadActions(int actionId)
        {
            var tablePath = ImporterSettings.instance.CharacterActionPath;

            if (!File.Exists(tablePath))
            {
                Debug.LogWarning($"[Top] missing '{tablePath}'");
                return null;
            }

            CharacterActionTable table;
            using (var stream = File.OpenRead(tablePath))
            {
                table = CharacterActionTable.Read(stream);
            }

            table.TryGetActions(actionId, out var actions);
            return actions;
        }

        public static void Convert(string modelPath, bool overwrite)
        {
            if (!ImporterSettings.RequireClientRoot())
            {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(modelPath);
            var kind = DetectKind(modelPath);

            if (kind == null)
            {
                Debug.LogError($"[Top] cannot derive model kind from '{modelPath}': " +
                               "expected a path under <...>/model/<kind>/");

                return;
            }

            var modelDir = $"{ContentRoot}/{kind}/Models/{name}";
            var prefabPath = $"{ContentRoot}/{kind}/Prefabs/{name}.prefab";

            if (!overwrite && File.Exists(prefabPath))
            {
                Debug.LogWarning($"[Top] {prefabPath} exists; skipped (enable overwrite to replace)");
                return;
            }

            var textureDir = $"{ContentTextureRoot}/{kind}";
            var textureSearchDir = Path.Combine(ImporterSettings.instance.TextureRoot, kind.ToLowerInvariant());
            var isLgo = modelPath.EndsWith(".lgo", StringComparison.OrdinalIgnoreCase);

            SceneModel model = null;
            GeometryObject single = null;

            try
            {
                using var stream = File.OpenRead(modelPath);

                if (isLgo)
                {
                    single = LgoFile.Read(stream).Object;
                }
                else
                {
                    model = LmoFile.Read(stream).Model;
                }
            }
            catch (MindPower.ParseException exception)
            {
                Debug.LogError($"[Top] failed to parse '{modelPath}': {exception.Message}");
                return;
            }

            var objects = isLgo ? new[] { single } : model.GeometryObjects;
            var litSubset = isLgo && kind == "Item" ? 1 : (int?)null;
            var hasBlendData = isLgo && single.Mesh.SkinBlends != null && single.Mesh.SkinBlends.Length > 0;
            var skeleton = hasBlendData ? LoadSkeleton(name) : null;

            ConversionResult result;

            if (!isLgo)
            {
                result = ModelConversion.Convert(model, name, textureSearchDir,
                    Path.GetFullPath(modelDir), Path.GetFullPath(textureDir));
            }
            else if (skeleton != null)
            {
                result = ModelConversion.Convert(single, skeleton, name, textureSearchDir,
                    Path.GetFullPath(modelDir), Path.GetFullPath(textureDir));
            }
            else
            {
                result = ModelConversion.Convert(single, name, textureSearchDir,
                    Path.GetFullPath(modelDir), Path.GetFullPath(textureDir),
                    litSubset: litSubset);
            }

            ScaffoldModel(modelPath, name, kind, objects, result, modelDir, prefabPath,
                textureDir, overwrite);
        }

        private static void ScaffoldModel(string sourcePath, string name, string kind,
            GeometryObject[] objects, ConversionResult result, string modelDir,
            string prefabPath, string textureDir, bool overwrite)
        {
            foreach (var warning in result.Warnings)
            {
                Debug.LogWarning($"[Top] {warning}");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var png in result.TexturePngPaths)
            {
                var assetPath = $"{textureDir}/{Path.GetFileName(png)}";

                if (AssetImporter.GetAtPath(assetPath) is TextureImporter textureImporter &&
                    !textureImporter.alphaIsTransparency)
                {
                    textureImporter.alphaIsTransparency = true;
                    textureImporter.SaveAndReimport();
                }
            }

            ConfigureAlphaTestedTextures(objects, textureDir);

            var glbAssetPath = $"{modelDir}/{name}.glb";
            var materialsByObject = CreateMaterials(objects, name, modelDir, textureDir, overwrite);

            RegisterMaterialRemaps(glbAssetPath, materialsByObject);

            var uvTracks = CreateUvTracks(objects, name, modelDir, overwrite);
            var imageTracks = CreateTextureImageTracks(objects, name, modelDir, textureDir, overwrite);
            var opacityTracks = CreateOpacityTracks(objects, name, modelDir, overwrite);

            BuildPrefab(sourcePath, prefabPath, glbAssetPath, uvTracks, imageTracks, opacityTracks, kind);

            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(prefabPath);

            Debug.Log($"[Top] scaffolded {prefabPath}");
        }

        private static BoneAnimation LoadSkeleton(string modelName)
        {
            if (modelName.Length < 4)
            {
                return null;
            }

            var labPath = Path.Combine(ImporterSettings.instance.AnimationRoot, modelName.Substring(0, 4) + ".lab");

            if (!File.Exists(labPath))
            {
                Debug.LogWarning($"[Top] no skeleton at '{labPath}'; converting rigid");
                return null;
            }

            try
            {
                using var stream = File.OpenRead(labPath);

                return LabFile.Read(stream).Animation;
            }
            catch (MindPower.ParseException exception)
            {
                Debug.LogWarning($"[Top] failed to parse '{labPath}': {exception.Message}");

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
                    var kind = Path.GetFileName(dir).ToLowerInvariant();
                    return char.ToUpperInvariant(kind[0]) + kind.Substring(1);
                }
            }

            return null;
        }

        /// <summary>
        /// Alpha-tested cutouts fringe at lower mips: averaging spreads the
        /// alpha edge, and near-zero cutoffs let the dilated fill color
        /// through. The engine sidestepped this by loading such textures
        /// without mip chains; here the mips instead preserve coverage at
        /// the material's own cutoff. A texture shared by several alpha
        /// cutoffs keeps the smallest one.
        /// </summary>
        private static void ConfigureAlphaTestedTextures(GeometryObject[] objects, string textureDir)
        {
            var cutoffs = new Dictionary<string, float>();

            foreach (var obj in objects)
            {
                for (var i = 0; i < (obj.Materials?.Length ?? 0); i++)
                {
                    var state = RenderStateResolver.Resolve(obj, i);

                    if (!state.AlphaTest || state.TextureFile == null)
                    {
                        continue;
                    }

                    var png = Path.GetFileNameWithoutExtension(state.TextureFile) + ".png";

                    if (!cutoffs.TryGetValue(png, out var existing) || state.Cutoff < existing)
                    {
                        cutoffs[png] = state.Cutoff;
                    }
                }
            }

            foreach (var pair in cutoffs)
            {
                var assetPath = $"{textureDir}/{pair.Key}";

                if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer
                    && (!importer.mipMapsPreserveCoverage
                        || !Mathf.Approximately(importer.alphaTestReferenceValue, pair.Value)))
                {
                    importer.mipMapsPreserveCoverage = true;
                    importer.alphaTestReferenceValue = pair.Value;
                    importer.SaveAndReimport();
                }
            }
        }

        private static Dictionary<uint, Material[]> CreateMaterials(
            GeometryObject[] objects, string name, string modelDir, string textureDir,
            bool overwrite)
        {
            var materialsDir = $"{modelDir}/Materials";
            Directory.CreateDirectory(materialsDir);
            var result = new Dictionary<uint, Material[]>();

            foreach (var obj in objects)
            {
                var materials = new Material[obj.Materials?.Length ?? 0];
                for (var i = 0; i < materials.Length; i++)
                {
                    var assetPath = $"{materialsDir}/{GltfContract.Material(name, obj.Id, i)}.mat";
                    var existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (existing != null && !overwrite)
                    {
                        materials[i] = existing;
                        continue;
                    }

                    var spec = RenderStateMapper.ToSpec(RenderStateResolver.Resolve(obj, i));

                    Texture2D texture = null;
                    if (spec.TextureFile != null)
                    {
                        var pngPath = $"{textureDir}/" +
                                      Path.GetFileNameWithoutExtension(spec.TextureFile) + ".png";
                        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
                        if (texture == null)
                        {
                            Debug.LogWarning($"[Top] texture missing: {pngPath}");
                        }
                    }

                    var material = RenderStateMapper.CreateMaterial(spec, texture);
                    material.name = Path.GetFileNameWithoutExtension(assetPath);
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(material, existing);
                        UnityEngine.Object.DestroyImmediate(material);
                        materials[i] = existing;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(material, assetPath);
                        materials[i] = material;
                    }
                }

                result[obj.Id] = materials;
            }

            return result;
        }

        private static Dictionary<(uint, int), UvAnimationTrack> CreateUvTracks(
            GeometryObject[] objects, string name, string modelDir, bool overwrite)
        {
            var result = new Dictionary<(uint, int), UvAnimationTrack>();
            var tracksDir = $"{modelDir}/Tracks";

            foreach (var obj in objects)
            {
                var trackSet = obj.Animation?.TextureUv;
                if (trackSet == null)
                {
                    continue;
                }

                for (var subset = 0; subset < trackSet.GetLength(0); subset++)
                {
                    for (var stage = 0; stage < trackSet.GetLength(1); stage++)
                    {
                        var animation = trackSet[subset, stage];
                        if (animation == null || animation.Frames == null
                                              || animation.Frames.Length == 0)
                        {
                            continue;
                        }

                        if (obj.Materials == null || subset >= obj.Materials.Length)
                        {
                            Debug.LogWarning(
                                $"[Top] UV animation on subset {subset} exceeds material count (obj {obj.Id})");
                            continue;
                        }

                        if (stage != 0)
                        {
                            Debug.LogWarning(
                                $"[Top] UV animation on stage {stage} ignored (obj {obj.Id})");
                            continue;
                        }

                        Directory.CreateDirectory(tracksDir);
                        var assetPath = $"{tracksDir}/{name}_uv_{obj.Id}_{subset}.asset";
                        var existing = AssetDatabase.LoadAssetAtPath<UvAnimationTrack>(assetPath);
                        if (existing != null && !overwrite)
                        {
                            result[(obj.Id, subset)] = existing;
                            continue;
                        }

                        var track = ScriptableObject.CreateInstance<UvAnimationTrack>();
                        track.name = Path.GetFileNameWithoutExtension(assetPath);
                        track.framesPerSecond = GltfContract.AnimationFramesPerSecond;
                        track.frames = animation.Frames.Select(ToUnityMatrix).ToArray();
                        if (existing != null)
                        {
                            EditorUtility.CopySerialized(track, existing);
                            UnityEngine.Object.DestroyImmediate(track);
                            result[(obj.Id, subset)] = existing;
                        }
                        else
                        {
                            AssetDatabase.CreateAsset(track, assetPath);
                            result[(obj.Id, subset)] = track;
                        }
                    }
                }
            }

            return result;
        }

        private static Dictionary<(uint, int), TextureImageTrack> CreateTextureImageTracks(
            GeometryObject[] objects, string name, string modelDir, string textureDir,
            bool overwrite)
        {
            var result = new Dictionary<(uint, int), TextureImageTrack>();
            var tracksDir = $"{modelDir}/Tracks";

            foreach (var obj in objects)
            {
                var trackSet = obj.Animation?.TextureImage;
                if (trackSet == null)
                {
                    continue;
                }

                for (var subset = 0; subset < trackSet.GetLength(0); subset++)
                {
                    for (var stage = 0; stage < trackSet.GetLength(1); stage++)
                    {
                        var animation = trackSet[subset, stage];
                        if (animation == null || animation.DataSequence == null
                                              || animation.DataSequence.Length == 0)
                        {
                            continue;
                        }

                        if (obj.Materials == null || subset >= obj.Materials.Length)
                        {
                            Debug.LogWarning(
                                $"[Top] texture-image animation on subset {subset} exceeds material count (obj {obj.Id})");
                            continue;
                        }

                        if (stage != 0)
                        {
                            Debug.LogWarning(
                                $"[Top] texture-image animation on stage {stage} ignored (obj {obj.Id})");
                            continue;
                        }

                        Directory.CreateDirectory(tracksDir);
                        var assetPath = $"{tracksDir}/{name}_teximg_{obj.Id}_{subset}.asset";
                        var existing = AssetDatabase.LoadAssetAtPath<TextureImageTrack>(assetPath);
                        if (existing != null && !overwrite)
                        {
                            result[(obj.Id, subset)] = existing;
                            continue;
                        }

                        var track = ScriptableObject.CreateInstance<TextureImageTrack>();
                        track.name = Path.GetFileNameWithoutExtension(assetPath);
                        track.framesPerSecond = GltfContract.AnimationFramesPerSecond;
                        track.frames = animation.DataSequence
                            .Select(frame => LoadFrameTexture(frame, textureDir, obj.Id))
                            .ToArray();
                        if (existing != null)
                        {
                            EditorUtility.CopySerialized(track, existing);
                            UnityEngine.Object.DestroyImmediate(track);
                            result[(obj.Id, subset)] = existing;
                        }
                        else
                        {
                            AssetDatabase.CreateAsset(track, assetPath);
                            result[(obj.Id, subset)] = track;
                        }
                    }
                }
            }

            return result;
        }

        private static Dictionary<(uint, int), OpacityAnimationTrack> CreateOpacityTracks(
            GeometryObject[] objects, string name, string modelDir, bool overwrite)
        {
            var result = new Dictionary<(uint, int), OpacityAnimationTrack>();
            var tracksDir = $"{modelDir}/Tracks";

            foreach (var obj in objects)
            {
                var trackSet = obj.Animation?.MaterialOpacity;
                if (trackSet == null)
                {
                    continue;
                }

                for (var subset = 0; subset < trackSet.Length; subset++)
                {
                    var animation = trackSet[subset];
                    if (animation == null || animation.Keys == null
                                          || animation.Keys.Length == 0)
                    {
                        continue;
                    }

                    if (obj.Materials == null || subset >= obj.Materials.Length)
                    {
                        Debug.LogWarning(
                            $"[Top] opacity animation on subset {subset} exceeds material count (obj {obj.Id})");
                        continue;
                    }

                    foreach (var key in animation.Keys)
                    {
                        if (key.SlerpType != 1)
                        {
                            Debug.LogWarning(
                                $"[Top] opacity key slerp type {key.SlerpType} treated as linear (obj {obj.Id})");
                        }
                    }

                    Directory.CreateDirectory(tracksDir);
                    var assetPath = $"{tracksDir}/{name}_opacity_{obj.Id}_{subset}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<OpacityAnimationTrack>(assetPath);
                    if (existing != null && !overwrite)
                    {
                        result[(obj.Id, subset)] = existing;
                        continue;
                    }

                    var track = ScriptableObject.CreateInstance<OpacityAnimationTrack>();
                    track.name = Path.GetFileNameWithoutExtension(assetPath);
                    track.framesPerSecond = GltfContract.AnimationFramesPerSecond;
                    track.keyFrames = animation.Keys.Select(k => (int)k.Key).ToArray();
                    track.values = animation.Keys.Select(k => k.Value).ToArray();
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(track, existing);
                        UnityEngine.Object.DestroyImmediate(track);
                        result[(obj.Id, subset)] = existing;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(track, assetPath);
                        result[(obj.Id, subset)] = track;
                    }
                }
            }

            return result;
        }

        private static Texture2D LoadFrameTexture(TextureStage frame, string textureDir, uint objectId)
        {
            if (frame == null || string.IsNullOrEmpty(frame.FileName))
            {
                return null;
            }

            var pngPath = $"{textureDir}/" +
                          Path.GetFileNameWithoutExtension(frame.FileName) + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
            if (texture == null)
            {
                Debug.LogWarning($"[Top] flipbook texture missing: {pngPath} (obj {objectId})");
            }

            return texture;
        }

        private static void RegisterMaterialRemaps(
            string glbAssetPath, Dictionary<uint, Material[]> materialsByObject)
        {
            var importer = AssetImporter.GetAtPath(glbAssetPath);
            foreach (var materials in materialsByObject.Values)
            {
                foreach (var material in materials)
                {
                    if (material != null)
                    {
                        importer.AddRemap(
                            new AssetImporter.SourceAssetIdentifier(
                                typeof(Material), material.name),
                            material);
                    }
                }
            }

            AssetDatabase.WriteImportSettingsIfDirty(glbAssetPath);
            AssetDatabase.ImportAsset(glbAssetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private static void BuildPrefab(string modelPath, string prefabPath,
            string glbAssetPath, Dictionary<(uint, int), UvAnimationTrack> uvTracks,
            Dictionary<(uint, int), TextureImageTrack> imageTracks,
            Dictionary<(uint, int), OpacityAnimationTrack> opacityTracks, string kind)
        {
            var modelRoot = AssetDatabase.LoadMainAssetAtPath(glbAssetPath) as GameObject;
            if (modelRoot == null)
            {
                Debug.LogError($"[Top] no imported model at '{glbAssetPath}'");
                return;
            }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(modelRoot);
            try
            {
                foreach (var entry in uvTracks)
                {
                    var nodeName = GltfContract.Geometry(entry.Key.Item1);
                    var target = FindDeep(root.transform, nodeName);
                    if (target == null)
                    {
                        Debug.LogWarning($"[Top] UV-animated node {nodeName} not found");
                        continue;
                    }

                    var uvComponent = target.gameObject.AddComponent<UvMatrixAnimation>();
                    uvComponent.materialIndex = entry.Key.Item2;
                    uvComponent.track = entry.Value;
                }

                foreach (var entry in imageTracks)
                {
                    var nodeName = GltfContract.Geometry(entry.Key.Item1);
                    var target = FindDeep(root.transform, nodeName);
                    if (target == null)
                    {
                        Debug.LogWarning($"[Top] flipbook node {nodeName} not found");
                        continue;
                    }

                    var component = target.gameObject.AddComponent<TextureImageAnimation>();
                    component.materialIndex = entry.Key.Item2;
                    component.track = entry.Value;
                }

                foreach (var entry in opacityTracks)
                {
                    var nodeName = GltfContract.Geometry(entry.Key.Item1);
                    var target = FindDeep(root.transform, nodeName);
                    if (target == null)
                    {
                        Debug.LogWarning($"[Top] opacity-animated node {nodeName} not found");
                        continue;
                    }

                    var component = target.gameObject.AddComponent<OpacityAnimation>();
                    component.materialIndex = entry.Key.Item2;
                    component.track = entry.Value;
                }

                foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if (GltfContract.IsHelper(meshFilter.gameObject.name))
                    {
                        meshFilter.gameObject.AddComponent<MeshCollider>().sharedMesh =
                            meshFilter.sharedMesh;
                    }
                }

                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GltfContract.IsLitShell(transform.name))
                    {
                        transform.gameObject.SetActive(false);
                    }
                }

                if (kind == "Scene")
                {
                    var sceneObject = root.AddComponent<SceneObject>();
                    sceneObject.typeId = LookupTypeId(Path.GetFileName(modelPath));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform != root && transform.name == name)
                {
                    return transform;
                }
            }

            return null;
        }

        private static Matrix4x4 ToUnityMatrix(System.Numerics.Matrix4x4 m)
        {
            var u = Matrix4x4.identity;
            u.m00 = m.M11;
            u.m01 = m.M12;
            u.m10 = m.M21;
            u.m11 = m.M22;
            u.m20 = m.M31;
            u.m21 = m.M32;
            return u;
        }

        private static int LookupTypeId(string modelFileName)
        {
            var table = LoadSceneObjectInfo();
            var row = table?.FirstOrDefault(record =>
                string.Equals(record.Name, modelFileName, StringComparison.OrdinalIgnoreCase));

            return row?.Id ?? -1;
        }
    }
}
