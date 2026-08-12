using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Top.Conversion.Gltf
{
    /// <summary>
    /// Appends binary data to buffer 0 of a document, creating aligned
    /// bufferViews and accessors. Call Finish once to get the bin blob.
    /// </summary>
    public class BufferBuilder
    {
        private readonly GltfDocument _doc;
        private readonly MemoryStream _bin = new MemoryStream();
        private readonly BinaryWriter _writer;

        public BufferBuilder(GltfDocument doc)
        {
            _doc = doc;
            _writer = new BinaryWriter(_bin);
            _doc.Buffers ??= new List<GltfBuffer>();

            if (_doc.Buffers.Count == 0)
            {
                _doc.Buffers.Add(new GltfBuffer());
            }

            _doc.BufferViews ??= new List<GltfBufferView>();
            _doc.Accessors ??= new List<GltfAccessor>();
        }

        public int AddVec3(Vector3[] data, bool withMinMax, bool vertexData = true)
        {
            var min = new[] { float.MaxValue, float.MaxValue, float.MaxValue };
            var max = new[] { float.MinValue, float.MinValue, float.MinValue };
            var view = BeginView(data.Length * 12, vertexData ? GltfConst.ArrayBuffer : null);

            foreach (var v in data)
            {
                _writer.Write(v.X);
                _writer.Write(v.Y);
                _writer.Write(v.Z);

                if (withMinMax)
                {
                    if (v.X < min[0])
                    {
                        min[0] = v.X;
                    }

                    if (v.Y < min[1])
                    {
                        min[1] = v.Y;
                    }

                    if (v.Z < min[2])
                    {
                        min[2] = v.Z;
                    }

                    if (v.X > max[0])
                    {
                        max[0] = v.X;
                    }

                    if (v.Y > max[1])
                    {
                        max[1] = v.Y;
                    }

                    if (v.Z > max[2])
                    {
                        max[2] = v.Z;
                    }
                }
            }

            return AddAccessor(view, GltfConst.Float, data.Length, "VEC3",
                withMinMax ? min : null, withMinMax ? max : null, normalized: false);
        }

        public int AddVec2(Vector2[] data)
        {
            var view = BeginView(data.Length * 8, GltfConst.ArrayBuffer);

            foreach (var v in data)
            {
                _writer.Write(v.X);
                _writer.Write(v.Y);
            }

            return AddAccessor(view, GltfConst.Float, data.Length, "VEC2",
                null, null, normalized: false);
        }

        public int AddScalars(float[] values, bool withMinMax)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            var view = BeginView(values.Length * 4, null);

            foreach (var value in values)
            {
                _writer.Write(value);

                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }

            return AddAccessor(view, GltfConst.Float, values.Length, "SCALAR",
                withMinMax ? new[] { min } : null,
                withMinMax ? new[] { max } : null, normalized: false);
        }

        public int AddVec4(Vector4[] data, bool vertexData = false)
        {
            var view = BeginView(data.Length * 16, vertexData ? GltfConst.ArrayBuffer : null);

            foreach (var v in data)
            {
                _writer.Write(v.X);
                _writer.Write(v.Y);
                _writer.Write(v.Z);
                _writer.Write(v.W);
            }

            return AddAccessor(view, GltfConst.Float, data.Length, "VEC4",
                null, null, normalized: false);
        }

        public int AddJoints(ushort[] joints)
        {
            var view = BeginView(joints.Length * 2, GltfConst.ArrayBuffer);

            foreach (var j in joints)
            {
                _writer.Write(j);
            }

            return AddAccessor(view, GltfConst.UnsignedShort, joints.Length / 4, "VEC4",
                null, null, normalized: false);
        }

        public int AddMatrices(Matrix4x4[] matrices)
        {
            var view = BeginView(matrices.Length * 64, null);

            foreach (var m in matrices)
            {
                _writer.Write(m.M11);
                _writer.Write(m.M12);
                _writer.Write(m.M13);
                _writer.Write(m.M14);
                _writer.Write(m.M21);
                _writer.Write(m.M22);
                _writer.Write(m.M23);
                _writer.Write(m.M24);
                _writer.Write(m.M31);
                _writer.Write(m.M32);
                _writer.Write(m.M33);
                _writer.Write(m.M34);
                _writer.Write(m.M41);
                _writer.Write(m.M42);
                _writer.Write(m.M43);
                _writer.Write(m.M44);
            }

            return AddAccessor(view, GltfConst.Float, matrices.Length, "MAT4",
                null, null, normalized: false);
        }

        public int AddColors(byte[] rgba, int count)
        {
            var view = BeginView(rgba.Length, GltfConst.ArrayBuffer);

            _writer.Write(rgba);

            return AddAccessor(view, GltfConst.UnsignedByte, count, "VEC4",
                null, null, normalized: true);
        }

        public int AddIndices(uint[] indices)
        {
            var wide = indices.Any(i => i > ushort.MaxValue);

            var view = BeginView(indices.Length * (wide ? 4 : 2), GltfConst.ElementArrayBuffer);

            foreach (var i in indices)
            {
                if (wide)
                {
                    _writer.Write(i);
                }
                else
                {
                    _writer.Write((ushort)i);
                }
            }

            return AddAccessor(view, wide ? GltfConst.UnsignedInt : GltfConst.UnsignedShort,
                indices.Length, "SCALAR", null, null, normalized: false);
        }

        public int AddBytes(byte[] data)
        {
            var view = BeginView(data.Length, null);

            _writer.Write(data);

            return view;
        }

        public byte[] Finish()
        {
            _writer.Flush();

            _doc.Buffers[0].ByteLength = (int)_bin.Length;

            return _bin.ToArray();
        }

        private int BeginView(int byteLength, int? target)
        {
            while (_bin.Length % 4 != 0)
            {
                _bin.WriteByte(0);
            }

            _doc.BufferViews.Add(new GltfBufferView
            {
                Buffer = 0,
                ByteOffset = (int)_bin.Length,
                ByteLength = byteLength,
                Target = target,
            });

            return _doc.BufferViews.Count - 1;
        }

        private int AddAccessor(int view, int componentType, int count, string type,
            float[] min, float[] max, bool normalized)
        {
            _doc.Accessors.Add(new GltfAccessor
            {
                BufferView = view,
                ComponentType = componentType,
                Count = count,
                Type = type,
                Min = min,
                Max = max,
                Normalized = normalized,
            });

            return _doc.Accessors.Count - 1;
        }
    }
}
