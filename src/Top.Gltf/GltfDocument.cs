using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Top.Gltf
{
    /// <summary>
    /// glTF 2.0 root. Unknown members survive in Rest
    /// </summary>
    public class GltfDocument
    {
        [JsonProperty("asset")] public GltfAsset Asset = new GltfAsset();
        [JsonProperty("scene")] public int? Scene;
        [JsonProperty("scenes")] public List<GltfScene> Scenes;
        [JsonProperty("nodes")] public List<GltfNode> Nodes;
        [JsonProperty("meshes")] public List<GltfMesh> Meshes;
        [JsonProperty("skins")] public List<GltfSkin> Skins;
        [JsonProperty("buffers")] public List<GltfBuffer> Buffers;
        [JsonProperty("bufferViews")] public List<GltfBufferView> BufferViews;
        [JsonProperty("accessors")] public List<GltfAccessor> Accessors;
        [JsonProperty("materials")] public List<GltfMaterial> Materials;
        [JsonProperty("textures")] public List<GltfTexture> Textures;
        [JsonProperty("images")] public List<GltfImage> Images;
        [JsonProperty("animations")] public List<GltfAnimation> Animations;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfAsset
    {
        [JsonProperty("version")] public string Version = "2.0";
        [JsonProperty("generator")] public string Generator;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }

    public class GltfScene
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("nodes")] public List<int> Nodes;
        [JsonProperty("extras")] public JToken Extras;
        [JsonExtensionData] public IDictionary<string, JToken> Rest;
    }
}
