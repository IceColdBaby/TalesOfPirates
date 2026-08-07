using System.Collections.Generic;
using System.Numerics;
using Top.Gltf;

namespace Top.Assets.Gltf.Tests
{
    internal static class TestGltf
    {
        public static GltfMeshRef Triangle(GltfBuilder builder, string meshName, string materialName)
        {
            var positions = builder.Buffer.AddVec3(new[]
            {
                new Vector3(1, 2, 3),
                new Vector3(4, 5, 6),
                new Vector3(7, 8, 9),
            }, withMinMax: true);

            return builder.AddMesh(meshName).AddPrimitive(
                new Dictionary<string, int> { ["POSITION"] = positions },
                builder.Buffer.AddIndices(new uint[] { 0, 1, 2 }),
                builder.AddMaterial(materialName));
        }

        public static (int Input, int Output) QuarterTurn(GltfBuilder builder)
        {
            return (
                builder.Buffer.AddScalars(new[] { 0f, 1f }, withMinMax: true),
                builder.Buffer.AddVec4(new[]
                {
                    new Vector4(0, 0, 0, 1),
                    new Vector4(0, 0.7071f, 0, 0.7071f),
                }));
        }
    }
}
