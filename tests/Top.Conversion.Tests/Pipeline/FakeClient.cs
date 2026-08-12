using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables.Custom;

namespace Top.Conversion.Tests.Pipeline
{
    /// <summary>
    /// A throwaway client tree beside a throwaway output root: original files
    /// go in by copying fixtures, converted ones come out where the settings
    /// say. Both are gone when the test ends.
    /// </summary>
    internal class FakeClient : IDisposable
    {
        private readonly string _root;

        internal FakeClient()
        {
            _root = Path.Combine(Path.GetTempPath(), "top-pipeline-tests",
                TestContext.CurrentContext.Test.ID);

            Delete();

            ClientRoot = Path.Combine(_root, "client");
            OutputRoot = Path.Combine(_root, "Content");

            Directory.CreateDirectory(ClientRoot);
            Directory.CreateDirectory(OutputRoot);
        }

        internal string ClientRoot { get; }

        internal string OutputRoot { get; }

        internal ConversionSettings Settings(bool overwrite = true)
        {
            return new ConversionSettings(ClientRoot, OutputRoot, overwrite);
        }

        /// <summary>
        /// Copies a fixture model into one of the client's model folders and
        /// returns where it landed.
        /// </summary>
        internal string AddModel(string kind, string fixture, string fileName = null)
        {
            var source = Fixtures.Path(fixture);
            var path = Path.Combine(ClientRoot, "model", kind,
                fileName ?? Path.GetFileName(source));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(source, path, overwrite: true);

            return path;
        }

        internal string AddSkeleton(string fixture, string fileName = null)
        {
            var source = Fixtures.Path(fixture);
            var path = Path.Combine(ClientRoot, "animation", fileName ?? Path.GetFileName(source));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(source, path, overwrite: true);

            return path;
        }

        internal void AddTextures(string kind, string fixtureDir)
        {
            var target = Path.Combine(ClientRoot, "texture", kind);

            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(Fixtures.Path(fixtureDir)))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
            }
        }

        internal string Converted(string kind, string name)
        {
            return Path.Combine(OutputRoot, kind, "Models", name, name + ".glb");
        }

        internal string ConvertedRig(string name)
        {
            return Path.Combine(OutputRoot, "Character", "Rigs", name, name + ".glb");
        }

        /// <summary>
        /// An action table holding one clip for one action set.
        /// </summary>
        internal static CharacterActionTable ActionSet(int id)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes($"{id}\n\t1 0 10 0 5\n"));

            return CharacterActionTable.Read(stream);
        }

        public void Dispose()
        {
            Delete();
        }

        private void Delete()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
