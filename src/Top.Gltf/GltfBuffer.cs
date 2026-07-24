using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Gltf
{
    public class GltfBuffer
    {
        [JsonProperty("uri")] public string Uri;
        [JsonProperty("byteLength")] public int ByteLength;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfBufferView
    {
        [JsonProperty("buffer")] public int Buffer;
        [JsonProperty("byteOffset")] public int ByteOffset;
        [JsonProperty("byteLength")] public int ByteLength;
        [JsonProperty("byteStride")] public int? ByteStride;
        [JsonProperty("target")] public int? Target;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfAccessor
    {
        [JsonProperty("bufferView")] public int? BufferView;
        [JsonProperty("byteOffset")] public int ByteOffset;
        [JsonProperty("componentType")] public int ComponentType;

        [JsonProperty("normalized", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Normalized;

        [JsonProperty("count")] public int Count;
        [JsonProperty("type")] public string Type;
        [JsonProperty("min")] public float[] Min;
        [JsonProperty("max")] public float[] Max;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public static class GltfConst
    {
        public const int Byte = 5120;
        public const int UnsignedByte = 5121;
        public const int Short = 5122;
        public const int UnsignedShort = 5123;
        public const int UnsignedInt = 5125;
        public const int Float = 5126;
        public const int ArrayBuffer = 34962;
        public const int ElementArrayBuffer = 34963;
        public const int Triangles = 4;
    }
}
