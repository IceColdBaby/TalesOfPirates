using NUnit.Framework;

namespace Top.MindPower.Tests
{
    internal static class Fixtures
    {
        internal static string FixturesPath => System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "fixtures");

        internal static string Path(string relative)
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(FixturesPath, relative));
        }
    }
}
