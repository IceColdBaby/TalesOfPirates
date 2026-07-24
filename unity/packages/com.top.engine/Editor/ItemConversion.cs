using System;
using System.IO;
using Top.Tables.Custom;
using Top.Tables.Records;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor
{
    /// <summary>
    /// Id-driven item conversion: resolves an iteminfo row's
    /// per-framework modules to client source files, converts them
    /// through the model pipeline, and writes the item's definition
    /// asset referencing the converted assets.
    /// </summary>
    public static class ItemConversion
    {
        public static void ConvertItem(int id, bool overwrite)
        {
            if (!ImporterSettings.RequireClientRoot())
            {
                return;
            }

            var items = TopModelConverter.LoadItemInfo();

            if (items == null)
            {
                return;
            }

            if (!items.TryGetById(id, out var item))
            {
                Debug.LogError($"[Top] no iteminfo row {id}");
                return;
            }

            var anyModule = false;

            for (var model = 0; model < 4; model++)
            {
                if (!ItemModules.TryGetModule(item, model, out var module))
                {
                    continue;
                }

                anyModule = true;

                if (!overwrite && LoadConvertedModule(item, module) != null)
                {
                    continue;
                }

                var sourcePath = FindModuleSource(item, module);

                if (sourcePath == null)
                {
                    Debug.LogWarning($"[Top] item {item.Id} '{item.Name}': no source file " +
                        $"for module {module}");
                    continue;
                }

                TopModelConverter.Convert(sourcePath, overwrite);
            }

            if (!anyModule)
            {
                Debug.LogWarning($"[Top] item {item.Id} '{item.Name}' has no models for any framework");
            }

            EnsureDefinition(item);
        }

        /// <summary>
        /// Client source for a module, in the folder the item's type
        /// dictates: body-part wearables under model/character, held or
        /// attached meshes under model/item. Falls back to the other
        /// folder with a warning - legacy rows carry a wrong type but
        /// real files. Null when neither file exists.
        /// </summary>
        internal static string FindModuleSource(ItemInfoRecord item, string module)
        {
            var preferred = ItemModules.IsWearable(item.Type) ? "character" : "item";
            var other = preferred == "character" ? "item" : "character";
            var path = Path.Combine(ImporterSettings.instance.ModelRoot, preferred, module + ".lgo");

            if (File.Exists(path))
            {
                return path;
            }

            path = Path.Combine(ImporterSettings.instance.ModelRoot, other, module + ".lgo");

            if (File.Exists(path))
            {
                Debug.LogWarning($"[Top] item {item.Id} '{item.Name}' (type {item.Type}): module " +
                    $"{module} found under model/{other} instead of model/{preferred}");
                return path;
            }

            return null;
        }

        /// <summary>
        /// The converted asset a module resolves to, preferring the kind
        /// the item's type dictates: the wearable part GLB, or the held
        /// item prefab. Accepts the other kind when only that exists.
        /// </summary>
        internal static GameObject LoadConvertedModule(ItemInfoRecord item, string module)
        {
            var part = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Content/Character/Models/{module}/{module}.glb");
            var held = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Content/Item/Prefabs/{module}.prefab");

            if (ItemModules.IsWearable(item.Type))
            {
                return part != null ? part : held;
            }

            return held != null ? held : part;
        }

        internal static void EnsureDefinition(ItemInfoRecord item)
        {
            EnsureDefinition(item, module => LoadConvertedModule(item, module));
        }

        internal static void EnsureDefinition(ItemInfoRecord item, Func<string, GameObject> loadModule)
        {
            var models = new GameObject[4];
            var any = false;

            for (var model = 0; model < 4; model++)
            {
                if (!ItemModules.TryGetModule(item, model, out var module))
                {
                    continue;
                }

                models[model] = loadModule(module);
                any |= models[model] != null;
            }

            if (!any)
            {
                return;
            }

            var assetPath = ItemDefinition.AssetPath(item.Id);
            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            var fresh = definition == null;

            if (fresh)
            {
                definition = ScriptableObject.CreateInstance<ItemDefinition>();
            }

            definition.id = item.Id;
            definition.itemName = item.Name;
            definition.models = models;

            if (fresh)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                AssetDatabase.CreateAsset(definition, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);
            }
        }
    }
}
