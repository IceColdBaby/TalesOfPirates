using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Top.Tables.Custom;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor
{
    /// <summary>
    /// Table-driven batch conversion: enumerates characterinfo, iteminfo
    /// and sceneobjinfo and feeds each referenced file to the existing
    /// conversion entry points, with per-unit fault isolation, a
    /// cancelable progress bar, and a per-category summary. Converting
    /// what the tables reference - and only that - doubles as the
    /// used-asset filter over the original client.
    /// </summary>
    public static class BatchConverter
    {
        private static bool _canceled;

        public static void Run(bool characters, bool items, bool sceneObjects, bool overwrite)
        {
            if (!ImporterSettings.RequireClientRoot())
            {
                return;
            }

            _canceled = false;

            try
            {
                if (characters && !_canceled)
                {
                    RunCharacters(overwrite);
                }

                if (items && !_canceled)
                {
                    RunItems(overwrite);
                }

                if (sceneObjects && !_canceled)
                {
                    RunSceneObjects(overwrite);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (_canceled)
            {
                Debug.Log("[Top] batch canceled");
            }
        }

        private static void RunCharacters(bool overwrite)
        {
            var table = TopModelConverter.LoadCharacterInfo();

            if (table == null)
            {
                Debug.LogError("[Top] batch characters aborted: characterinfo table missing");
                return;
            }

            var models = table.Select(record => record.Model).Distinct().OrderBy(model => model).ToList();
            int processed = 0, failed = 0;

            for (var i = 0; i < models.Count; i++)
            {
                if (Cancel("Characters", $"model {models[i]:D4}", i, models.Count))
                {
                    break;
                }

                var labPath = Path.Combine(ImporterSettings.instance.AnimationRoot, $"{models[i]:D4}.lab");

                if (!File.Exists(labPath))
                {
                    Debug.LogWarning($"[Top] missing '{labPath}'");
                    failed++;
                    continue;
                }

                try
                {
                    TopModelConverter.ConvertCharacter(labPath, overwrite);
                    processed++;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Top] model {models[i]:D4} failed: {exception}");
                    failed++;
                }
            }

            Debug.Log($"[Top] batch characters: {processed} processed, {failed} failed " +
                $"(of {models.Count} models)");
        }

        private static void RunItems(bool overwrite)
        {
            var items = TopModelConverter.LoadItemInfo();

            if (items == null)
            {
                Debug.LogError("[Top] batch items aborted: iteminfo table missing");
                return;
            }

            var rows = items.ToList();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int converted = 0, skipped = 0, failed = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                if (Cancel("Items", $"item {rows[i].Id}", i, rows.Count))
                {
                    break;
                }

                for (var model = 0; model < 4; model++)
                {
                    if (!ItemModules.TryGetModule(rows[i], model, out var module) || !seen.Add(module))
                    {
                        continue;
                    }

                    if (!overwrite && ItemConversion.LoadConvertedModule(rows[i], module) != null)
                    {
                        skipped++;
                        continue;
                    }

                    var source = ItemConversion.FindModuleSource(rows[i], module);

                    if (source == null)
                    {
                        Debug.LogWarning($"[Top] item {rows[i].Id} '{rows[i].Name}': no source file " +
                            $"for module {module}");
                        failed++;
                        continue;
                    }

                    try
                    {
                        TopModelConverter.Convert(source, overwrite);
                        converted++;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[Top] module {module} failed: {exception}");
                        failed++;
                    }
                }

                try
                {
                    ItemConversion.EnsureDefinition(rows[i]);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Top] definition for item {rows[i].Id} failed: {exception}");
                    failed++;
                }
            }

            Debug.Log($"[Top] batch items: {converted} converted, {skipped} skipped, {failed} failed " +
                $"(of {seen.Count} modules across {rows.Count} rows)");
        }

        private static void RunSceneObjects(bool overwrite)
        {
            var table = TopModelConverter.LoadSceneObjectInfo();

            if (table == null)
            {
                Debug.LogError("[Top] batch scene objects aborted: sceneobjinfo table missing");
                return;
            }

            var rows = table.Where(record => !string.IsNullOrEmpty(record.Name)).ToList();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int converted = 0, skipped = 0, failed = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                if (Cancel("Scene objects", rows[i].Name, i, rows.Count))
                {
                    break;
                }

                if (!seen.Add(rows[i].Name))
                {
                    continue;
                }

                var prefabPath = "Assets/Content/Scene/Prefabs/" +
                    Path.GetFileNameWithoutExtension(rows[i].Name) + ".prefab";

                if (!overwrite && File.Exists(prefabPath))
                {
                    skipped++;
                    continue;
                }

                var source = Path.Combine(ImporterSettings.instance.ModelRoot, "scene", rows[i].Name);

                if (!File.Exists(source))
                {
                    Debug.LogWarning($"[Top] missing '{source}'");
                    failed++;
                    continue;
                }

                try
                {
                    TopModelConverter.Convert(source, overwrite);
                    converted++;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[Top] {rows[i].Name} failed: {exception}");
                    failed++;
                }
            }

            Debug.Log($"[Top] batch scene objects: {converted} converted, {skipped} skipped, " +
                $"{failed} failed (of {rows.Count} rows)");
        }

        private static bool Cancel(string category, string info, int index, int count)
        {
            if (EditorUtility.DisplayCancelableProgressBar(
                    $"Top Importer - {category}", info, (float)index / count))
            {
                _canceled = true;
            }

            return _canceled;
        }
    }
}
