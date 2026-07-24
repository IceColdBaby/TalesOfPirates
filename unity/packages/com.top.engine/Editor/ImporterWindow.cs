using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor
{
    /// <summary>
    /// Central entry point for client asset conversion: the client root
    /// setting, single conversions by file pick or game id, and the
    /// table-driven full-client batch.
    /// </summary>
    public sealed class ImporterWindow : EditorWindow
    {
        private int _characterModel;
        private int _itemId;
        private int _sceneObjectId;
        private bool _batchCharacters = true;
        private bool _batchItems = true;
        private bool _batchSceneObjects = true;

        [MenuItem("Top/Importer")]
        public static void Open()
        {
            GetWindow<ImporterWindow>("Top Importer");
        }

        private void OnGUI()
        {
            DrawSettings();
            EditorGUILayout.Space();
            DrawFileConversion();
            EditorGUILayout.Space();
            DrawTableConversion();
            EditorGUILayout.Space();
            DrawBatch();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            var settings = ImporterSettings.instance;

            using (new EditorGUILayout.HorizontalScope())
            {
                var root = EditorGUILayout.TextField("Client root", settings.ClientRoot);

                if (root != settings.ClientRoot)
                {
                    settings.ClientRoot = root;
                }

                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var picked = EditorUtility.OpenFolderPanel(
                        "Original client assets root", settings.ClientRoot, string.Empty);

                    if (!string.IsNullOrEmpty(picked))
                    {
                        settings.ClientRoot = picked;
                        GUI.FocusControl(null);
                    }
                }
            }

            EditorGUILayout.LabelField(" ", Status(settings), EditorStyles.miniLabel);

            ImporterSettings.instance.Overwrite =
                EditorGUILayout.ToggleLeft("Overwrite existing", ImporterSettings.instance.Overwrite);
        }

        private static string Status(ImporterSettings settings)
        {
            if (!settings.IsValid)
            {
                return "client root not found";
            }

            var missing = new[] { "model", "animation", "texture", "scripts" }
                .Where(dir => !Directory.Exists(Path.Combine(settings.ClientRoot, dir)))
                .ToList();

            return missing.Count == 0
                ? "all expected subfolders found"
                : "missing: " + string.Join(", ", missing);
        }

        private void DrawFileConversion()
        {
            EditorGUILayout.LabelField("File conversion", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Model (.lmo/.lgo)..."))
                {
                    var path = EditorUtility.OpenFilePanelWithFilters("Select model",
                        ImporterSettings.instance.ModelRoot, new[] { "Model files", "lmo,lgo" });

                    if (!string.IsNullOrEmpty(path))
                    {
                        TopModelConverter.Convert(path, ImporterSettings.instance.Overwrite);
                    }
                }

                if (GUILayout.Button("Rig (.lab)..."))
                {
                    var path = EditorUtility.OpenFilePanelWithFilters("Select skeleton",
                        ImporterSettings.instance.AnimationRoot, new[] { "Skeleton files", "lab" });

                    if (!string.IsNullOrEmpty(path))
                    {
                        TopModelConverter.ConvertRig(path);
                    }
                }

                if (GUILayout.Button("Character (.lab)..."))
                {
                    var path = EditorUtility.OpenFilePanelWithFilters("Select skeleton",
                        ImporterSettings.instance.AnimationRoot, new[] { "Skeleton files", "lab" });

                    if (!string.IsNullOrEmpty(path))
                    {
                        TopModelConverter.ConvertCharacter(path, ImporterSettings.instance.Overwrite);
                    }
                }
            }
        }

        private void DrawTableConversion()
        {
            EditorGUILayout.LabelField("Table conversion", EditorStyles.boldLabel);

            _characterModel = IdRow("Character model id", _characterModel, minimum: 0,
                convert: id => TopModelConverter.ConvertCharacterModel(id, ImporterSettings.instance.Overwrite));
            _itemId = IdRow("Item id", _itemId, minimum: 1,
                convert: id => ItemConversion.ConvertItem(id, ImporterSettings.instance.Overwrite));
            _sceneObjectId = IdRow("Scene object id", _sceneObjectId, minimum: 1,
                convert: id => TopModelConverter.ConvertSceneObject(id, ImporterSettings.instance.Overwrite));
        }

        private static int IdRow(string label, int value, int minimum, Action<int> convert)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                value = EditorGUILayout.IntField(label, value);

                using (new EditorGUI.DisabledScope(value < minimum))
                {
                    if (GUILayout.Button("Convert", GUILayout.Width(80)))
                    {
                        convert(value);
                    }
                }
            }

            return value;
        }

        private void DrawBatch()
        {
            EditorGUILayout.LabelField("Batch", EditorStyles.boldLabel);
            _batchCharacters = EditorGUILayout.ToggleLeft("Characters", _batchCharacters);
            _batchItems = EditorGUILayout.ToggleLeft("Items", _batchItems);
            _batchSceneObjects = EditorGUILayout.ToggleLeft("Scene objects", _batchSceneObjects);

            using (new EditorGUI.DisabledScope(!(_batchCharacters || _batchItems || _batchSceneObjects)))
            {
                if (GUILayout.Button("Convert client"))
                {
                    BatchConverter.Run(_batchCharacters, _batchItems, _batchSceneObjects,
                        ImporterSettings.instance.Overwrite);
                }
            }
        }
    }
}
