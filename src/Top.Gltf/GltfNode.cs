using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Gltf
{
    public class GltfNode
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("mesh")] public int? Mesh;
        [JsonProperty("skin")] public int? Skin;
        [JsonProperty("children")] public List<int> Children;
        [JsonProperty("matrix")] public float[] Matrix;
        [JsonProperty("translation")] public float[] Translation;
        [JsonProperty("rotation")] public float[] Rotation;
        [JsonProperty("scale")] public float[] Scale;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }
}
