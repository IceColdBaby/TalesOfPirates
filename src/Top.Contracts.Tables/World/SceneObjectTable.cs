using System.Collections.Generic;

namespace Top.Contracts.Tables.World
{
    public class SceneObjectTable : Table<SceneObjectEntry>
    {
        public const string TreePath = "tables/sceneobjects.json";

        public SceneObjectTable(IEnumerable<SceneObjectEntry> entries) : base(entries)
        {
        }
    }
}
