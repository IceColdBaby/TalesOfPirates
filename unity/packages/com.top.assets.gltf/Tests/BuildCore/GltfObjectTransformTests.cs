using NUnit.Framework;
using Top.Gltf;
using UnityEngine;

namespace Top.Assets.Gltf.Tests
{
    public class GltfObjectTransformTests
    {
        [Test]
        public void Sheared_matrix_nodes_decompose_best_effort()
        {
            var builder = new GltfBuilder("shear");

            builder
                .AddNode("sheared")
                .WithMatrix(new[]
                {
                    1f, 0f, 0f, 0f,
                    0.5f, 1f, 0f, 0f,
                    0f, 0f, 1f, 0f,
                    1f, 2f, 0f, 1f,
                })
                .AsRoot();

            var root = GltfObjectBuilder.Build(new GltfData(builder.Build()), "shear",
                System.Array.Empty<Material>(), null);

            try
            {
                var node = root.transform.Find("sheared");

                Assert.That(node.localPosition, Is.EqualTo(new Vector3(1f, 2f, 0f)));
                Assert.That(node.localScale.x, Is.EqualTo(1f).Within(1e-4f));
                Assert.That(node.localScale.y, Is.EqualTo(1.118034f).Within(1e-4f));
                Assert.That(node.localScale.z, Is.EqualTo(1f).Within(1e-4f));
                Assert.That(Quaternion.Angle(node.localRotation, Quaternion.Euler(0f, 0f, -26.565f)),
                    Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
