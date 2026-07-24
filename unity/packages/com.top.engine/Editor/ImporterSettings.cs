using System.IO;
using UnityEditor;
using UnityEngine;

namespace Top.Engine.Editor
{
    /// <summary>
    /// Per-machine importer configuration: the original client's asset
    /// root, the folder holding model/, texture/, animation/ and
    /// scripts/. Persisted under UserSettings/ (git-ignored), so every
    /// contributor points it at their own client install. Falls back to
    /// the repo-local reference/assets checkout when present.
    /// </summary>
    [FilePath("UserSettings/TopImporter.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class ImporterSettings : ScriptableSingleton<ImporterSettings>
    {
        [SerializeField]
        private string clientRoot = string.Empty;

        [SerializeField]
        private bool overwrite;

        public string ClientRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(clientRoot))
                {
                    return clientRoot;
                }

                var fallback = Path.GetFullPath("../../reference/assets");
                return Directory.Exists(fallback) ? fallback : string.Empty;
            }
            set
            {
                clientRoot = value;
                Save(true);
            }
        }

        /// <summary>
        /// Whether conversions replace existing output. Off preserves
        /// already-converted assets and their hand-edited materials.
        /// </summary>
        public bool Overwrite
        {
            get => overwrite;
            set
            {
                if (overwrite != value)
                {
                    overwrite = value;
                    Save(true);
                }
            }
        }

        internal string RawClientRoot => clientRoot;

        public bool IsValid => !string.IsNullOrEmpty(ClientRoot) && Directory.Exists(ClientRoot);

        public string ModelRoot => Path.Combine(ClientRoot, "model");

        public string TextureRoot => Path.Combine(ClientRoot, "texture");

        public string AnimationRoot => Path.Combine(ClientRoot, "animation");

        public string CharacterActionPath => Path.Combine(ClientRoot, "scripts", "txt", "CharacterAction.tx");

        public string TablePath(string name) => Path.Combine(ClientRoot, "scripts", "table", name);

        /// <summary>
        /// Gate for every conversion entry point: false (with one error
        /// naming the fix) when no usable client root is configured.
        /// </summary>
        public static bool RequireClientRoot()
        {
            if (instance.IsValid)
            {
                return true;
            }

            Debug.LogError("[Top] original client root is not set or does not exist; " +
                "configure it in Top/Importer");
            return false;
        }
    }
}
