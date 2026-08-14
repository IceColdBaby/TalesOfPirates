using System.IO;
using System.Threading.Tasks;
using Top.Content.Packs;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;

namespace Top.Client.Definitions
{
    /// <summary>
    /// Typed lookups over the converted tables, loaded once and immutable.
    /// </summary>
    public class DefinitionsDictionary
    {
        private DefinitionsDictionary(SceneObjectTable sceneObjects)
        {
            SceneObjects = sceneObjects;
        }

        public SceneObjectTable SceneObjects { get; }

        public static async Task<DefinitionsDictionary> Load(IComposedContent content)
        {
            var bytes = await content.Read(SceneObjectTable.TreePath);

            using var stream = new MemoryStream(bytes);
            var format = new TableFormat();

            return new DefinitionsDictionary(
                new SceneObjectTable(format.Read<SceneObjectEntry>(stream)));
        }
    }
}
