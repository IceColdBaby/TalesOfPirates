using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Gltf
{
    public class GltfMaterial
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("pbrMetallicRoughness")] public GltfPbrMetallicRoughness PbrMetallicRoughness;
        [JsonProperty("alphaMode")] public string AlphaMode;
        [JsonProperty("alphaCutoff")] public float? AlphaCutoff;

        [JsonProperty("doubleSided", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool DoubleSided;

        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfPbrMetallicRoughness
    {
        [JsonProperty("baseColorTexture")] public GltfTextureInfo BaseColorTexture;
        [JsonProperty("metallicFactor")] public float? MetallicFactor;
        [JsonProperty("roughnessFactor")] public float? RoughnessFactor;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfTextureInfo
    {
        [JsonProperty("index")] public int Index;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfTexture
    {
        [JsonProperty("source")] public int? Source;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfImage
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("uri")] public string Uri;
        [JsonProperty("mimeType")] public string MimeType;
        [JsonProperty("bufferView")] public int? BufferView;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }
}
