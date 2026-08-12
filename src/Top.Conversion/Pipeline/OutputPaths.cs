using System.IO;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The converted tree: a folder per model holding the glTF named after it,
    /// textures shared by every model of a kind.
    /// </summary>
    public class OutputPaths
    {
        private readonly string _root;

        public OutputPaths(string root)
        {
            _root = root;
        }

        public string ModelDir(string kind, string name) => Path.Combine(_root, kind, "Models", name);

        public string Model(string kind, string name) => Path.Combine(ModelDir(kind, name), name + ".glb");

        public string RigDir(string name) => Path.Combine(_root, ContentKind.Character, "Rigs", name);

        public string Rig(string name) => Path.Combine(RigDir(name), name + ".glb");

        public string TextureDir(string kind) => Path.Combine(_root, "Textures", kind);
    }
}
