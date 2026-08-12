using Newtonsoft.Json;

namespace Top.Conversion.Gltf
{
    public static class GltfJson
    {
        private static JsonSerializerSettings Settings => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static string Serialize(GltfDocument document, bool indented)
        {
            return JsonConvert.SerializeObject(document, indented ? Formatting.Indented : Formatting.None, Settings);
        }

        public static GltfDocument Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<GltfDocument>(json, Settings);
        }
    }
}
