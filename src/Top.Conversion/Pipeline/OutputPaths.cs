using System.IO;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The converted tree: one glTF file per model under its kind, rigs of
    /// their own, textures shared by every model of a kind. Every segment
    /// below the root is written lowercase.
    /// </summary>
    public class OutputPaths(string root)
    {
        public string At(string treePath) => Path.Combine(root, treePath.Replace('/', Path.DirectorySeparatorChar));

        public string Model(string kind, string name) => At(ModelTreePath(kind, name));

        public static string ModelTreePath(string kind, string name) => $"models/{Lower(kind)}/{Lower(name)}.glb";

        public string Rig(string name) => Path.Combine(root, "rigs", Lower(name) + ".glb");

        public string TextureDir(string kind) => Path.Combine(root, "textures", Lower(kind));

        private static string Lower(string segment) => segment.ToLowerInvariant();
    }
}
