using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Top.Conversion.Gltf.Tests
{
    public class ReaderTests
    {
        private static GltfFile RoundTripGlb()
        {
            (GltfDocument doc, byte[] bin) = WriterTests.BuildTriangle();

            using var ms = new MemoryStream();
            GltfWriter.WriteGlb(doc, bin, ms);
            ms.Position = 0;

            return GltfReader.Read(ms);
        }

        [Test]
        public void Reads_glb_back()
        {
            var file = RoundTripGlb();

            Assert.That(file.Document.Meshes[0].Name, Is.EqualTo("tri_mesh"));
            Assert.That(file.BinChunk, Is.Not.Null);
        }

        [Test]
        public void Decodes_positions_from_glb()
        {
            var file = RoundTripGlb();
            var data = new GltfData(file);
            var positions = data.ReadFloats(
                file.Document.Meshes[0].Primitives[0].Attributes["POSITION"]);

            Assert.That(positions, Is.EqualTo(new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }));
        }

        [Test]
        public void Decodes_indices_from_glb()
        {
            var file = RoundTripGlb();
            var data = new GltfData(file);

            Assert.That(data.ReadInts(file.Document.Meshes[0].Primitives[0].Indices!.Value),
                Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Reads_embedded_gltf()
        {
            (GltfDocument doc, byte[] bin) = WriterTests.BuildTriangle();

            using var ms = new MemoryStream();
            GltfWriter.WriteGltfEmbedded(doc, bin, ms);
            ms.Position = 0;

            var file = GltfReader.Read(ms);
            var data = new GltfData(file);

            Assert.That(data.ReadInts(file.Document.Meshes[0].Primitives[0].Indices!.Value),
                Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Reads_external_bin_via_resolver()
        {
            (GltfDocument doc, byte[] bin) = WriterTests.BuildTriangle();

            using var jsonStream = new MemoryStream();
            using var binStream = new MemoryStream();
            GltfWriter.WriteGltfWithBin(doc, bin, "tri.bin", jsonStream, binStream);
            jsonStream.Position = 0;

            var file = GltfReader.Read(jsonStream);
            var data = new GltfData(file, uri =>
            {
                Assert.That(uri, Is.EqualTo("tri.bin"));
                return binStream.ToArray();
            });

            Assert.That(data.ReadFloats(file.Document.Meshes[0].Primitives[0].Attributes["POSITION"]).Length,
                Is.EqualTo(9));
        }

        [Test]
        public void Decodes_normalized_unsigned_bytes()
        {
            var doc = new GltfDocument();
            var builder = new BufferBuilder(doc);
            var accessor = builder.AddColors([255, 0, 51, 255], count: 1);
            var bin = builder.Finish();
            var file = new GltfFile { Document = doc, BinChunk = bin };
            var data = new GltfData(file);

            var floats = data.ReadFloats(accessor);

            Assert.That(floats[0], Is.EqualTo(1f));
            Assert.That(floats[2], Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void Decodes_interleaved_buffer_views()
        {
            // Two vertices, interleaved POSITION (12 bytes) + 4 pad bytes, stride 16.
            var raw = new byte[32];

            void PutFloat(int offset, float value) => BitConverter.GetBytes(value).CopyTo(raw, offset);

            PutFloat(0, 1f);
            PutFloat(4, 2f);
            PutFloat(8, 3f);
            PutFloat(16, 4f);
            PutFloat(20, 5f);
            PutFloat(24, 6f);

            var doc = new GltfDocument
            {
                Buffers = [new GltfBuffer { ByteLength = 32 }],
                BufferViews =
                [
                    new GltfBufferView
                    {
                        Buffer = 0, ByteOffset = 0, ByteLength = 32, ByteStride = 16,
                    }
                ],
                Accessors =
                [
                    new GltfAccessor
                    {
                        BufferView = 0, ComponentType = GltfConst.Float, Count = 2, Type = "VEC3",
                    }
                ],
            };

            var data = new GltfData(new GltfFile { Document = doc, BinChunk = raw });

            Assert.That(data.ReadFloats(0), Is.EqualTo(new float[] { 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void Decodes_normalized_signed_components_with_clamp()
        {
            var raw = new byte[] { 0x80, 0x7F, 0x00, 0x80, 0xFF, 0x7F };
            var doc = new GltfDocument
            {
                Buffers = [new GltfBuffer { ByteLength = 6 }],
                BufferViews =
                [
                    new GltfBufferView { Buffer = 0, ByteOffset = 0, ByteLength = 2 },
                    new GltfBufferView { Buffer = 0, ByteOffset = 2, ByteLength = 4 }
                ],
                Accessors =
                [
                    new GltfAccessor
                    {
                        BufferView = 0, ComponentType = GltfConst.Byte,
                        Normalized = true, Count = 2, Type = "SCALAR",
                    },

                    new GltfAccessor
                    {
                        BufferView = 1, ComponentType = GltfConst.Short,
                        Normalized = true, Count = 2, Type = "SCALAR",
                    }
                ],
            };

            var data = new GltfData(new GltfFile { Document = doc, BinChunk = raw });

            Assert.That(data.ReadFloats(0), Is.EqualTo(new[] { -1f, 1f }));
            Assert.That(data.ReadFloats(1), Is.EqualTo(new[] { -1f, 1f }));
        }

        [Test]
        public void External_uri_is_percent_unescaped()
        {
            var doc = new GltfDocument
            {
                Buffers =
                [
                    new GltfBuffer { Uri = "my%20file.bin", ByteLength = 4 }
                ],
                BufferViews =
                [
                    new GltfBufferView { Buffer = 0, ByteOffset = 0, ByteLength = 4 }
                ],
                Accessors =
                [
                    new GltfAccessor
                    {
                        BufferView = 0, ComponentType = GltfConst.Float,
                        Count = 1, Type = "SCALAR",
                    }
                ],
            };

            string requested = null;

            var data = new GltfData(new GltfFile { Document = doc }, uri =>
            {
                requested = uri;
                return BitConverter.GetBytes(2.5f);
            });

            Assert.That(data.ReadFloats(0), Is.EqualTo(new[] { 2.5f }));
            Assert.That(requested, Is.EqualTo("my file.bin"));
        }

        [Test]
        public void Material_extras_survive_a_glb_round_trip()
        {
            (GltfDocument doc, byte[] bin) = WriterTests.BuildTriangle();
            doc.Materials =
            [
                new GltfMaterial
                {
                    Name = "mat",
                    Extras = new JObject { ["vendor"] = new JObject { ["depth"] = 3 } },
                }
            ];

            using var ms = new MemoryStream();
            GltfWriter.WriteGlb(doc, bin, ms);
            ms.Position = 0;

            var extras = GltfReader.Read(ms).Document.Materials[0].Extras;

            Assert.That(extras["vendor"]["depth"].Value<int>(), Is.EqualTo(3));
        }

        [Test]
        public void Reads_embedded_image_bytes()
        {
            var payload = new byte[] { 1, 2, 3, 4, 5 };
            var doc = new GltfDocument
            {
                Buffers = [new GltfBuffer { ByteLength = payload.Length }],
                BufferViews =
                [
                    new GltfBufferView { Buffer = 0, ByteOffset = 0, ByteLength = payload.Length }
                ],
                Images =
                [
                    new GltfImage { Name = "img", MimeType = "image/png", BufferView = 0 }
                ],
            };

            using var ms = new MemoryStream();
            GltfWriter.WriteGlb(doc, payload, ms);
            ms.Position = 0;

            var file = GltfReader.Read(ms);
            var data = new GltfData(file);
            var image = file.Document.Images[0];

            Assert.That(image.Name, Is.EqualTo("img"));
            Assert.That(image.MimeType, Is.EqualTo("image/png"));
            Assert.That(data.ReadBufferView(image.BufferView!.Value), Is.EqualTo(payload));
        }
    }
}
