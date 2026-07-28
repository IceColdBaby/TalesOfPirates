using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Top.Tables.Tests
{
    public class TableTests
    {
        private class StubRecord : TableRecord
        {
            public int Value;
        }

        private static Table<StubRecord> Build(params (int id, string name, int value)[] rows)
        {
            var records = rows
                .Select(r => new StubRecord { Id = r.id, Name = r.name, Value = r.value })
                .ToList();

            return new Table<StubRecord>(records);
        }

        [Test]
        public void Looks_up_by_id_and_name()
        {
            var table = Build((1, "one", 10), (2, "two", 20));

            Assert.That(table[2].Value, Is.EqualTo(20));
            Assert.That(table.TryGetById(1, out var byId), Is.True);
            Assert.That(byId.Value, Is.EqualTo(10));
            Assert.That(table.TryGetByName("two", out var byName), Is.True);
            Assert.That(byName.Value, Is.EqualTo(20));
        }

        [Test]
        public void Missing_lookups_report_false()
        {
            var table = Build((1, "one", 10));

            Assert.That(table.TryGetById(9, out _), Is.False);
            Assert.That(table.TryGetByName("nine", out _), Is.False);
            Assert.That(() => table[9], Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void Duplicate_id_keeps_the_last_record()
        {
            var table = Build((1, "a", 10), (1, "b", 20));

            Assert.That(table[1].Value, Is.EqualTo(20));
            Assert.That(table.Count, Is.EqualTo(2));
        }

        [Test]
        public void Duplicate_name_keeps_the_last_record()
        {
            var table = Build((1, "a", 10), (2, "a", 20));

            Assert.That(table.TryGetByName("a", out var byName), Is.True);
            Assert.That(byName.Id, Is.EqualTo(2));
            Assert.That(byName.Value, Is.EqualTo(20));
            Assert.That(table.Count, Is.EqualTo(2));
        }

        [Test]
        public void Enumerates_in_file_order()
        {
            var table = Build((5, "five", 50), (1, "one", 10));

            Assert.That(table.Select(r => r.Id), Is.EqualTo(new[] { 5, 1 }));
        }
    }
}
