using System;
using System.Collections.Generic;
using System.IO;

namespace Top.Conversion.Gltf
{
    /// <summary>
    /// Resolves buffers (GLB chunk, data: URIs, external via callback) and
    /// decodes accessors including strides and normalized integer types.
    /// </summary>
    public class GltfData
    {
        private readonly GltfFile _file;
        private readonly Func<string, byte[]> _resolveExternal;
        private readonly Dictionary<int, byte[]> _buffers = new Dictionary<int, byte[]>();

        public GltfData(GltfFile file, Func<string, byte[]> resolveExternal = null)
        {
            _file = file;
            _resolveExternal = resolveExternal;
        }

        public GltfDocument Document => _file.Document;

        public static int ComponentCount(string type)
        {
            return type switch
            {
                "SCALAR" => 1,
                "VEC2" => 2,
                "VEC3" => 3,
                "VEC4" => 4,
                "MAT4" => 16,
                _ => throw new InvalidDataException($"unsupported accessor type '{type}'"),
            };
        }

        public float[] ReadFloats(int accessorIndex)
        {
            var accessor = Document.Accessors[accessorIndex];
            var components = ComponentCount(accessor.Type);
            var result = new float[accessor.Count * components];

            Decode(accessor, components, (bytes, offset, k) =>
                result[k] = ToFloat(bytes, offset, accessor));

            return result;
        }

        public byte[] ReadBufferView(int bufferViewIndex)
        {
            var view = Document.BufferViews[bufferViewIndex];
            var buffer = GetBuffer(view.Buffer);
            var result = new byte[view.ByteLength];

            Array.Copy(buffer, view.ByteOffset, result, 0, view.ByteLength);

            return result;
        }

        public int[] ReadInts(int accessorIndex)
        {
            var accessor = Document.Accessors[accessorIndex];
            var components = ComponentCount(accessor.Type);
            var result = new int[accessor.Count * components];

            Decode(accessor, components, (bytes, offset, k) =>
                result[k] = ToInt(bytes, offset, accessor.ComponentType));

            return result;
        }

        private void Decode(GltfAccessor accessor, int components, Action<byte[], int, int> emit)
        {
            if (accessor.BufferView == null)
            {
                throw new InvalidDataException("accessors without bufferView are not supported");
            }

            var view = Document.BufferViews[accessor.BufferView.Value];
            var buffer = GetBuffer(view.Buffer);
            var componentSize = ComponentSize(accessor.ComponentType);
            var elementSize = componentSize * components;
            var stride = view.ByteStride ?? elementSize;
            var start = view.ByteOffset + accessor.ByteOffset;

            for (var i = 0; i < accessor.Count; i++)
            {
                for (var c = 0; c < components; c++)
                {
                    emit(buffer, start + (i * stride) + (c * componentSize), (i * components) + c);
                }
            }
        }

        private byte[] GetBuffer(int index)
        {
            if (_buffers.TryGetValue(index, out var cached))
            {
                return cached;
            }

            var buffer = Document.Buffers[index];
            byte[] bytes;

            if (buffer.Uri == null)
            {
                bytes = _file.BinChunk ?? throw new InvalidDataException("buffer has no uri and no GLB bin chunk");
            }
            else if (buffer.Uri.StartsWith("data:", StringComparison.Ordinal))
            {
                var comma = buffer.Uri.IndexOf(',');
                bytes = Convert.FromBase64String(buffer.Uri.Substring(comma + 1));
            }
            else
            {
                if (_resolveExternal == null)
                {
                    throw new InvalidDataException($"buffer uri '{buffer.Uri}' requires an external resolver");
                }

                bytes = _resolveExternal(Uri.UnescapeDataString(buffer.Uri));
            }

            _buffers[index] = bytes;

            return bytes;
        }

        private static int ComponentSize(int componentType)
        {
            return componentType switch
            {
                GltfConst.Byte => 1,
                GltfConst.UnsignedByte => 1,
                GltfConst.Short => 2,
                GltfConst.UnsignedShort => 2,
                GltfConst.UnsignedInt => 4,
                GltfConst.Float => 4,
                _ => throw new InvalidDataException($"unsupported componentType {componentType}"),
            };
        }

        private static float ToFloat(byte[] bytes, int offset, GltfAccessor accessor)
        {
            return accessor.ComponentType switch
            {
                GltfConst.Float => BitConverter.ToSingle(bytes, offset),
                GltfConst.UnsignedByte => accessor.Normalized
                    ? bytes[offset] / 255f
                    : bytes[offset],
                GltfConst.Byte => accessor.Normalized
                    ? Math.Max((sbyte)bytes[offset] / 127f, -1f)
                    : (sbyte)bytes[offset],
                GltfConst.UnsignedShort => accessor.Normalized
                    ? BitConverter.ToUInt16(bytes, offset) / 65535f
                    : BitConverter.ToUInt16(bytes, offset),
                GltfConst.Short => accessor.Normalized
                    ? Math.Max(BitConverter.ToInt16(bytes, offset) / 32767f, -1f)
                    : BitConverter.ToInt16(bytes, offset),
                GltfConst.UnsignedInt => BitConverter.ToUInt32(bytes, offset),
                _ => throw new InvalidDataException(
                    $"unsupported componentType {accessor.ComponentType}"),
            };
        }

        private static int ToInt(byte[] bytes, int offset, int componentType)
        {
            return componentType switch
            {
                GltfConst.UnsignedByte => bytes[offset],
                GltfConst.Byte => (sbyte)bytes[offset],
                GltfConst.UnsignedShort => BitConverter.ToUInt16(bytes, offset),
                GltfConst.Short => BitConverter.ToInt16(bytes, offset),
                GltfConst.UnsignedInt => (int)BitConverter.ToUInt32(bytes, offset),
                GltfConst.Float => (int)BitConverter.ToSingle(bytes, offset),
                _ => throw new InvalidDataException($"unsupported componentType {componentType}"),
            };
        }
    }
}
