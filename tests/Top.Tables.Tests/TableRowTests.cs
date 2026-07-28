using System.Numerics;
using NUnit.Framework;

namespace Top.Tables.Tests
{
    public class TableRowTests
    {
        private enum Sample
        {
            Zero = 0,
            Two = 2,
        }

        [Test]
        public void Exposes_line()
        {
            var row = new TableRow(1, ["7", "Mammoth "]);

            Assert.That(row.Line, Is.EqualTo(1));
        }

        [Test]
        public void Starts_at_the_first_column()
        {
            var row = new TableRow(1, ["7", "Mammoth"]);

            Assert.That(row.NextInt(), Is.EqualTo(7));
            Assert.That(row.NextString(), Is.EqualTo("Mammoth"));
        }

        [Test]
        public void Reads_columns_sequentially()
        {
            var row = new TableRow(1, ["icon", "3", "1.5", "1", "2"]);

            Assert.That(row.NextString(), Is.EqualTo("icon"));
            Assert.That(row.NextInt(), Is.EqualTo(3));
            Assert.That(row.NextFloat(), Is.EqualTo(1.5f));
            Assert.That(row.NextBool(), Is.True);
            Assert.That(row.NextEnum<Sample>(), Is.EqualTo(Sample.Two));
        }

        [Test]
        public void Reading_past_the_end_yields_defaults()
        {
            var row = new TableRow(1, ["7"]);

            Assert.That(row.NextString(), Is.EqualTo("7"));
            Assert.That(row.NextString(), Is.EqualTo(""));
            Assert.That(row.NextInt(), Is.EqualTo(0));
            Assert.That(row.NextBool(), Is.False);
        }

        [Test]
        public void Skip_consumes_columns()
        {
            var row = new TableRow(1, ["a", "b", "9"]);

            row.Skip(2);

            Assert.That(row.NextInt(), Is.EqualTo(9));
        }

        [Test]
        public void Skip_defaults_to_one_column()
        {
            var row = new TableRow(1, ["a", "9"]);

            row.Skip();

            Assert.That(row.NextInt(), Is.EqualTo(9));
        }

        [Test]
        public void Long_values_survive_beyond_int_range()
        {
            var row = new TableRow(1, ["4176709541"]);

            Assert.That(row.NextLong(), Is.EqualTo(4176709541L));
        }

        [Test]
        public void List_methods_split_one_comma_column()
        {
            var row = new TableRow(1, ["1,2,3", "x,y", ""]);

            Assert.That(row.NextIntList(), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(row.NextStringList(), Is.EqualTo(new[] { "x", "y" }));
            Assert.That(row.NextFloatList(), Is.Empty);
        }

        [Test]
        public void Counted_methods_consume_consecutive_columns()
        {
            var row = new TableRow(1, ["a", "b", "1", "2", "0.5", "1.5"]);

            Assert.That(row.NextStrings(2), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(row.NextInts(2), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(row.NextFloats(2), Is.EqualTo(new[] { 0.5f, 1.5f }));
        }

        [Test]
        public void Vector3_parses_one_column()
        {
            var row = new TableRow(1, ["1.0,2.0,3.0", "4.0"]);

            Assert.That(row.NextVector3(), Is.EqualTo(new Vector3(1f, 2f, 3f)));
            Assert.That(row.NextVector3(), Is.EqualTo(new Vector3(4f, 0f, 0f)));
        }
    }
}
