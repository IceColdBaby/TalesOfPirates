using System;
using System.Numerics;
using NUnit.Framework;

namespace Top.Conversion.Gltf.Tests
{
    public class AnimationSchemaTests
    {
        [Test]
        public void Animation_schema_round_trips()
        {
            var doc = new GltfDocument
            {
                Animations =
                [
                    new GltfAnimation
                    {
                        Name = "default",
                        Samplers =
                        {
                            new GltfAnimationSampler
                            {
                                Input = 0, Output = 1, Interpolation = "LINEAR",
                            },
                        },
                        Channels =
                        {
                            new GltfAnimationChannel
                            {
                                Sampler = 0,
                                Target = { Node = 3, Path = "rotation" },
                            },
                        },
                    }
                ],
            };

            var back = GltfJson.Deserialize(GltfJson.Serialize(doc, indented: false));

            Assert.That(back.Animations[0].Channels[0].Target.Node, Is.EqualTo(3));
            Assert.That(back.Animations[0].Channels[0].Target.Path, Is.EqualTo("rotation"));
            Assert.That(back.Animations[0].Samplers[0].Interpolation, Is.EqualTo("LINEAR"));
        }

        [Test]
        public void Scalar_and_vec4_accessors_have_expected_layout()
        {
            var doc = new GltfDocument();
            var builder = new BufferBuilder(doc);
            var times = builder.AddScalars([0f, 0.5f, 1f], withMinMax: true);
            var rotations = builder.AddVec4([new Vector4(0, 0, 0, 1)]);
            var bin = builder.Finish();

            Assert.That(doc.Accessors[times].Type, Is.EqualTo("SCALAR"));
            Assert.That(doc.Accessors[times].Min, Is.EqualTo(new[] { 0f }));
            Assert.That(doc.Accessors[times].Max, Is.EqualTo(new[] { 1f }));
            Assert.That(doc.BufferViews[doc.Accessors[times].BufferView!.Value].Target, Is.Null);
            Assert.That(doc.Accessors[rotations].Type, Is.EqualTo("VEC4"));
            Assert.That(BitConverter.ToSingle(bin,
                    doc.BufferViews[doc.Accessors[rotations].BufferView!.Value].ByteOffset + 12),
                Is.EqualTo(1f));
        }
    }
}
