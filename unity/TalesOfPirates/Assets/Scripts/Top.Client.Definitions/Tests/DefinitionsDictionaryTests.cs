using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Content.Packs;
using Top.Contracts.Tables.World;

namespace Top.Client.Definitions.Tests
{
    public class DefinitionsDictionaryTests
    {
        private static MemoryContent Content(string json)
        {
            var content = new MemoryContent();

            content.Add(SceneObjectTable.TreePath, Encoding.UTF8.GetBytes(json));

            return content;
        }

        [Test]
        public async Task Table_bytes_load_into_lookups_by_id()
        {
            var definitions = await DefinitionsDictionary.Load(Content(
                "[{\"id\":42,\"modelPath\":\"models/scene/stone01.glb\"}," +
                "{\"id\":7,\"type\":3,\"color\":[255,128,0]}]"));

            Assert.That(definitions.SceneObjects.Count, Is.EqualTo(2));
            Assert.That(definitions.SceneObjects[42].ModelPath, Is.EqualTo("models/scene/stone01.glb"));
            Assert.That(definitions.SceneObjects[7].ModelPath, Is.Null, "an emitter row carries no path");
            Assert.That(((PointLightEntry)definitions.SceneObjects[7]).Color,
                Is.EqualTo(new[] { 255, 128, 0 }));
        }

        [Test]
        public void An_unknown_id_looks_up_as_absent()
        {
            var definitions = DefinitionsDictionary.Load(Content("[{\"id\":42}]"))
                .GetAwaiter().GetResult();

            Assert.That(definitions.SceneObjects.TryGetById(9000, out _), Is.False);
        }

        [Test]
        public void Missing_table_content_fails_the_load()
        {
            Assert.ThrowsAsync<FileNotFoundException>(
                () => DefinitionsDictionary.Load(new MemoryContent()));
        }
    }
}
