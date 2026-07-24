using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Gltf
{
    public class GltfAnimation
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("channels")] public List<GltfAnimationChannel> Channels = new List<GltfAnimationChannel>();
        [JsonProperty("samplers")] public List<GltfAnimationSampler> Samplers = new List<GltfAnimationSampler>();
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfAnimationChannel
    {
        [JsonProperty("sampler")] public int Sampler;
        [JsonProperty("target")] public GltfAnimationTarget Target = new GltfAnimationTarget();
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfAnimationTarget
    {
        [JsonProperty("node")] public int? Node;
        [JsonProperty("path")] public string Path;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfAnimationSampler
    {
        [JsonProperty("input")] public int Input;
        [JsonProperty("interpolation")] public string Interpolation;
        [JsonProperty("output")] public int Output;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }
}
