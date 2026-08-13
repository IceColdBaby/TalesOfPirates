using System.IO;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The converted tree: one glTF file per model under its kind, rigs of
    /// their own, textures shared by every model of a kind. Every segment
    /// below the root is written lowercase.
    /// </summary>
    public class OutputPaths
    {
        private readonly string _root;

        public OutputPaths(string root)
        {
            _root = root;
        }

        public string Model(string kind, string name) =>
            Path.Combine(_root, "models", Lower(kind), Lower(name) + ".glb");

        public string Rig(string name) => Path.Combine(_root, "rigs", Lower(name) + ".glb");

        public string TextureDir(string kind) => Path.Combine(_root, "textures", Lower(kind));

        private static string Lower(string segment) => segment.ToLowerInvariant();
    }
}
