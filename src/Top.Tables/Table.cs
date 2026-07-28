using System.Collections;
using System.Collections.Generic;

namespace Top.Tables
{
    /// <summary>
    /// A loaded row table: lookup by id and by name, enumeration in file
    /// order. Duplicate ids or names keep the last row, as the original
    /// loader overwrote its slot and name index.
    /// </summary>
    public class Table<T> : IReadOnlyCollection<T> where T : TableRecord
    {
        private readonly List<T> _records;
        private readonly Dictionary<int, T> _byId;
        private readonly Dictionary<string, T> _byName;

        public Table(List<T> records)
        {
            _records = records;
            _byId = new Dictionary<int, T>(records.Count);
            _byName = new Dictionary<string, T>(records.Count);

            foreach (var record in records)
            {
                _byId[record.Id] = record;
                _byName[record.Name] = record;
            }
        }

        public int Count => _records.Count;

        public T this[int id] => _byId[id];

        public bool TryGetById(int id, out T record)
        {
            return _byId.TryGetValue(id, out record);
        }

        public bool TryGetByName(string name, out T record)
        {
            return _byName.TryGetValue(name, out record);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _records.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
