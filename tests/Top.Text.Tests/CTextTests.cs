using NUnit.Framework;

namespace Top.Text.Tests
{
    public class CTextTests
    {
        [TestCase("42", 42)]
        [TestCase("-7", -7)]
        [TestCase("+3", 3)]
        [TestCase("12abc", 12)]
        [TestCase("abc", 0)]
        [TestCase("", 0)]
        [TestCase(null, 0)]
        [TestCase("  8", 8)]
        public void Atoi_matches_c_semantics(string text, int expected)
        {
            Assert.That(CText.Atoi(text), Is.EqualTo(expected));
        }

        [TestCase("4176709541", 4176709541L)]
        [TestCase("-7", -7L)]
        [TestCase("abc", 0L)]
        public void AtoiLong_matches_atoi64_semantics(string text, long expected)
        {
            Assert.That(CText.AtoiLong(text), Is.EqualTo(expected));
        }

        [TestCase("1.5", 1.5f)]
        [TestCase("-0.25", -0.25f)]
        [TestCase("2.5e2", 250f)]
        [TestCase("3x", 3f)]
        [TestCase("x", 0f)]
        [TestCase("", 0f)]
        public void Atof_matches_c_semantics(string text, float expected)
        {
            Assert.That(CText.Atof(text), Is.EqualTo(expected).Within(1e-6f));
        }
    }
}
