using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Conversion.Gltf
{
    public class GltfMesh
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("primitives")] public List<GltfPrimitive> Primitives = new List<GltfPrimitive>();
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfPrimitive
    {
        [JsonProperty("attributes")] public Dictionary<string, int> Attributes = new Dictionary<string, int>();
        [JsonProperty("indices")] public int? Indices;
        [JsonProperty("material")] public int? Material;
        [JsonProperty("mode")] public int? Mode;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }
}
