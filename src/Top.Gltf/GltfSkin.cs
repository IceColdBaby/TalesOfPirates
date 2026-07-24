using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Gltf
{
    public class GltfSkin
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("inverseBindMatrices")] public int? InverseBindMatrices;
        [JsonProperty("skeleton")] public int? Skeleton;
        [JsonProperty("joints")] public List<int> Joints = new List<int>();
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }
}
