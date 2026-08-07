using System.Collections.Generic;
using NUnit.Framework;
using Top.Gltf;
using UnityEngine;

namespace Top.Assets.Gltf.Tests
{
    /// <summary>
    /// The Unity half of the axis change, pinned to literals.
    /// <br/>
    /// The reference model below is shared with the conversion golden test in
    /// <c>Top.Assets.Conversion.Tests</c>, which pins the MindPower column
    /// against the glTF one. Read the two together and a value can be chained
    /// end to end.
    /// <code>
    ///           MindPower       glTF            Unity
    /// vertex    1, 2, 3         1, 3, 2         1, 3, -2
    /// normal    0.6, 0.8, 0     0.6, 0, 0.8     0.6, 0, -0.8
    /// uv        0.25, 0.75      0.25, 0.75      0.25, 0.25
    /// triangle  0, 1, 2         0, 2, 1         0, 1, 2
    /// origin    10, 20, 30      10, 30, 20      10, 30, -20
    /// </code>
    /// </summary>
    public class AxisGoldenTests
    {
        private static GameObject Build()
        {
            var builder = new GltfBuilder("reference");
            var mesh = builder.AddMesh("geom_0");

            mesh.AddPrimitive(
                new Dictionary<string, int>
                {
                    ["POSITION"] = builder.Buffer.AddVec3(new[]
                    {
                        new System.Numerics.Vector3(1, 3, 2),
                        new System.Numerics.Vector3(0, 0, 0),
                        new System.Numerics.Vector3(0, 0, 0),
                    }, withMinMax: true),
                    ["NORMAL"] = builder.Buffer.AddVec3(new[]
                    {
                        new System.Numerics.Vector3(0.6f, 0f, 0.8f),
                        new System.Numerics.Vector3(0f, 1f, 0f),
                        new System.Numerics.Vector3(0f, 1f, 0f),
                    }, withMinMax: false),
                    ["TEXCOORD_0"] = builder.Buffer.AddVec2(new[]
                    {
                        new System.Numerics.Vector2(0.25f, 0.75f),
                        new System.Numerics.Vector2(0f, 0f),
                        new System.Numerics.Vector2(0f, 0f),
                    }),
                },
                builder.Buffer.AddIndices(new uint[] { 0, 2, 1 }));

            builder
                .AddNode("geom_0")
                .WithMesh(mesh)
                .WithMatrix(new[]
                {
                    1f, 0f, 0f, 0f,
                    0f, 1f, 0f, 0f,
                    0f, 0f, 1f, 0f,
                    10f, 30f, 20f, 1f,
                })
                .AsRoot();

            return GltfObjectBuilder.Build(new GltfData(builder.Build()), "reference",
                System.Array.Empty<Material>(), null);
        }

        private static Mesh MeshOf(GameObject root)
        {
            return root.GetComponentInChildren<MeshFilter>(true).sharedMesh;
        }

        [Test]
        public void The_reference_vertex_lands_on_its_Unity_numbers()
        {
            var root = Build();

            try
            {
                Assert.That(MeshOf(root).vertices[0], Is.EqualTo(new Vector3(1f, 3f, -2f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void The_reference_normal_lands_on_its_Unity_numbers()
        {
            var root = Build();

            try
            {
                var normal = MeshOf(root).normals[0];

                Assert.That(normal.x, Is.EqualTo(0.6f).Within(1e-6f));
                Assert.That(normal.y, Is.EqualTo(0f).Within(1e-6f));
                Assert.That(normal.z, Is.EqualTo(-0.8f).Within(1e-6f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void The_reference_coordinate_flips_its_second_axis()
        {
            var root = Build();

            try
            {
                Assert.That(MeshOf(root).uv[0], Is.EqualTo(new Vector2(0.25f, 0.25f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void The_reference_triangle_winds_back_the_way_it_started()
        {
            var root = Build();

            try
            {
                Assert.That(MeshOf(root).triangles, Is.EqualTo(new[] { 0, 1, 2 }));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void The_reference_origin_lands_on_its_Unity_numbers()
        {
            var root = Build();

            try
            {
                Assert.That(root.transform.Find("geom_0").localPosition,
                    Is.EqualTo(new Vector3(10f, 30f, -20f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
