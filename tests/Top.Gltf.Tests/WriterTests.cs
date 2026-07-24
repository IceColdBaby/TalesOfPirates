using System;
using System.IO;
using System.Numerics;
using System.Text;
using NUnit.Framework;

namespace Top.Gltf.Tests
{
    public class WriterTests
    {
        internal static (GltfDocument Doc, byte[] Bin) BuildTriangle()
        {
            var doc = new GltfDocument
            {
                Scene = 0,
                Scenes =
                [
                    new GltfScene { Nodes = [0] }
                ],
                Nodes =
                [
                    new GltfNode { Name = "tri", Mesh = 0 }
                ],
                Meshes =
                [
                    new GltfMesh { Name = "tri_mesh" }
                ],
            };
            var builder = new BufferBuilder(doc);
            var positions = builder.AddVec3([new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)],
                withMinMax: true);
            var indices = builder.AddIndices([0, 1, 2]);

            doc.Meshes[0].Primitives.Add(new GltfPrimitive
            {
                Attributes = { ["POSITION"] = positions },
                Indices = indices,
            });

            return (doc, builder.Finish());
        }

        [Test]
        public void Glb_layout_is_spec_conformant()
        {
            (GltfDocument doc, byte[] bin) = BuildTriangle();

            using var ms = new MemoryStream();
            GltfWriter.WriteGlb(doc, bin, ms);
            var bytes = ms.ToArray();

            Assert.That(BitConverter.ToUInt32(bytes, 0), Is.EqualTo(0x46546C67u), "magic");
            Assert.That(BitConverter.ToUInt32(bytes, 4), Is.EqualTo(2u), "version");
            Assert.That(BitConverter.ToUInt32(bytes, 8), Is.EqualTo((uint)bytes.Length), "total length");
            var jsonLength = BitConverter.ToInt32(bytes, 12);
            Assert.That(jsonLength % 4, Is.EqualTo(0), "JSON chunk padded to 4");
            Assert.That(BitConverter.ToUInt32(bytes, 16), Is.EqualTo(0x4E4F534Au), "JSON chunk type");
            var binChunkStart = 20 + jsonLength;
            Assert.That(BitConverter.ToUInt32(bytes, binChunkStart + 4), Is.EqualTo(0x004E4942u), "BIN chunk type");
        }

        [Test]
        public void Position_accessor_has_min_max()
        {
            (GltfDocument doc, _) = BuildTriangle();
            var accessor = doc.Accessors[doc.Meshes[0].Primitives[0].Attributes["POSITION"]];

            Assert.That(accessor.Min, Is.EqualTo(new float[] { 0, 0, 0 }));
            Assert.That(accessor.Max, Is.EqualTo(new float[] { 1, 1, 0 }));
            Assert.That(accessor.ComponentType, Is.EqualTo(GltfConst.Float));
            Assert.That(accessor.Type, Is.EqualTo("VEC3"));
        }

        [Test]
        public void Small_indices_use_unsigned_short()
        {
            (GltfDocument doc, _) = BuildTriangle();

            var accessor = doc.Accessors[doc.Meshes[0].Primitives[0].Indices!.Value];

            Assert.That(accessor.ComponentType, Is.EqualTo(GltfConst.UnsignedShort));
            Assert.That(accessor.Count, Is.EqualTo(3));
        }

        [Test]
        public void Embedded_gltf_uses_base64_data_uri()
        {
            (GltfDocument doc, byte[] bin) = BuildTriangle();

            using var ms = new MemoryStream();
            GltfWriter.WriteGltfEmbedded(doc, bin, ms);
            var json = Encoding.UTF8.GetString(ms.ToArray());

            Assert.That(json, Does.Contain("data:application/octet-stream;base64,"));
        }

        [Test]
        public void External_bin_layout_writes_uri_and_bytes()
        {
            (GltfDocument doc, byte[] bin) = BuildTriangle();

            using var jsonStream = new MemoryStream();
            using var binStream = new MemoryStream();
            GltfWriter.WriteGltfWithBin(doc, bin, "tri.bin", jsonStream, binStream);

            Assert.That(Encoding.UTF8.GetString(jsonStream.ToArray()),
                Does.Contain("\"uri\": \"tri.bin\""));
            Assert.That(binStream.ToArray(), Is.EqualTo(bin));
        }

        [Test]
        public void Buffer_views_are_4_byte_aligned()
        {
            (GltfDocument doc, _) = BuildTriangle();

            foreach (var view in doc.BufferViews)
            {
                Assert.That(view.ByteOffset % 4, Is.EqualTo(0));
            }
        }

        [Test]
        public void Vec2_and_colors_layouts_are_correct()
        {
            var doc = new GltfDocument();
            var builder = new BufferBuilder(doc);
            var uv = builder.AddVec2([new Vector2(0.25f, 0.75f)]);
            var colors = builder.AddColors([1, 2, 3, 4, 5, 6, 7, 8], count: 2);
            var bin = builder.Finish();

            var uvAccessor = doc.Accessors[uv];
            Assert.That(uvAccessor.Type, Is.EqualTo("VEC2"));
            Assert.That(uvAccessor.ComponentType, Is.EqualTo(GltfConst.Float));
            Assert.That(uvAccessor.Count, Is.EqualTo(1));

            var colorAccessor = doc.Accessors[colors];
            Assert.That(colorAccessor.Type, Is.EqualTo("VEC4"));
            Assert.That(colorAccessor.ComponentType, Is.EqualTo(GltfConst.UnsignedByte));
            Assert.That(colorAccessor.Normalized, Is.True);
            Assert.That(colorAccessor.Count, Is.EqualTo(2));

            Assert.That(BitConverter.ToSingle(bin, doc.BufferViews[uvAccessor.BufferView!.Value].ByteOffset),
                Is.EqualTo(0.25f));
            Assert.That(bin[doc.BufferViews[colorAccessor.BufferView!.Value].ByteOffset], Is.EqualTo(1));
        }

        [Test]
        public void Skinning_accessors_have_spec_layouts()
        {
            var doc = new GltfDocument();
            var builder = new BufferBuilder(doc);
            var joints = builder.AddJoints([0, 1, 2, 3, 4, 5, 6, 7]);
            var weights = builder.AddVec4([new Vector4(0.5f, 0.5f, 0, 0)], vertexData: true);
            var matrices = builder.AddMatrices([Matrix4x4.Identity]);
            var bin = builder.Finish();

            var jointAccessor = doc.Accessors[joints];
            Assert.That(jointAccessor.Type, Is.EqualTo("VEC4"));
            Assert.That(jointAccessor.ComponentType, Is.EqualTo(GltfConst.UnsignedShort));
            Assert.That(jointAccessor.Count, Is.EqualTo(2));
            Assert.That(doc.BufferViews[jointAccessor.BufferView!.Value].Target,
                Is.EqualTo(GltfConst.ArrayBuffer));

            var weightAccessor = doc.Accessors[weights];
            Assert.That(weightAccessor.Type, Is.EqualTo("VEC4"));
            Assert.That(weightAccessor.ComponentType, Is.EqualTo(GltfConst.Float));
            Assert.That(doc.BufferViews[weightAccessor.BufferView!.Value].Target,
                Is.EqualTo(GltfConst.ArrayBuffer));

            var matrixAccessor = doc.Accessors[matrices];
            Assert.That(matrixAccessor.Type, Is.EqualTo("MAT4"));
            Assert.That(matrixAccessor.ComponentType, Is.EqualTo(GltfConst.Float));
            Assert.That(matrixAccessor.Count, Is.EqualTo(1));
            Assert.That(doc.BufferViews[matrixAccessor.BufferView!.Value].Target, Is.Null);

            var data = new GltfData(new GltfFile { Document = doc, BinChunk = bin });
            Assert.That(data.ReadInts(joints), Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            var identity = data.ReadFloats(matrices);
            Assert.That(identity.Length, Is.EqualTo(16));
            Assert.That(identity[0], Is.EqualTo(1f));
            Assert.That(identity[5], Is.EqualTo(1f));
            Assert.That(identity[10], Is.EqualTo(1f));
            Assert.That(identity[15], Is.EqualTo(1f));
            Assert.That(identity[1], Is.EqualTo(0f));
        }

        [Test]
        public void Large_indices_use_unsigned_int()
        {
            var doc = new GltfDocument();
            var builder = new BufferBuilder(doc);
            var accessor = builder.AddIndices([0, 70000, 2]);
            var bin = builder.Finish();

            Assert.That(doc.Accessors[accessor].ComponentType, Is.EqualTo(GltfConst.UnsignedInt));
            Assert.That(BitConverter.ToUInt32(bin, 4), Is.EqualTo(70000u));
        }

        [Test]
        public void Unaligned_view_is_padded_before_next_view()
        {
            var doc = new GltfDocument();
            var builder = new BufferBuilder(doc);
            builder.AddIndices([0, 1, 2]);
            var positions = builder.AddVec3([new Vector3(1, 2, 3)], withMinMax: false);
            builder.Finish();

            Assert.That(doc.BufferViews[doc.Accessors[positions].BufferView!.Value].ByteOffset, Is.EqualTo(8));
        }
    }
}
