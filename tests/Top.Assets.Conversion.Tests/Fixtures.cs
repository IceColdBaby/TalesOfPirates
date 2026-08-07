using System.Linq;
using System.Numerics;
using System.Reflection;
using Top.MindPower.Geometry;

namespace Top.Assets.Conversion.Tests
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

        /// <summary>
        /// One triangle at the origin, no materials and no animation.
        /// </summary>
        internal static GeometryObject MakeObject(uint id, uint parentId)
        {
            return new GeometryObject
            {
                Id = id,
                ParentId = parentId,
                LocalMatrix = Matrix4x4.Identity,
                Mesh = new Mesh
                {
                    PointType = 4,
                    Vertices =
                    [
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(0, 1, 0)
                    ],
                    Indices = [0, 1, 2],
                    Subsets = [new MeshSubset { PrimitiveCount = 1, StartIndex = 0 }],
                },
            };
        }
    }
}
