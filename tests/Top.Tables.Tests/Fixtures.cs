using System.Linq;
using System.Reflection;

namespace Top.Tables.Tests
{
    internal static class Fixtures
    {
        internal static string FixturesPath => typeof(Fixtures).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(a => a.Key == "FixturesPath").Value;

        internal static string Path(string relative)
        {
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(FixturesPath, relative));
        }
    }
}
