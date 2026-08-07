using System.IO;

namespace Top.Assets.Conversion.Pipeline
{
    /// <summary>
    /// Where the original client keeps each kind of file.
    /// </summary>
    public class SourcePaths
    {
        private readonly string _root;

        public SourcePaths(string root)
        {
            _root = root;
        }

        public string Models => Path.Combine(_root, "model");

        public string Textures => Path.Combine(_root, "texture");

        public string Animations => Path.Combine(_root, "animation");

        public string CharacterAction => Path.Combine(_root, "scripts", "txt", "CharacterAction.tx");

        public string Model(string folder, string fileName) => Path.Combine(Models, folder, fileName);

        public string TextureDir(string kind) => Path.Combine(Textures, kind.ToLowerInvariant());

        public string Skeleton(int model) => Skeleton($"{model:D4}");

        public string Skeleton(string name) => Path.Combine(Animations, name + ".lab");

        public string Table(string name) => Path.Combine(_root, "scripts", "table", name);
    }
}
