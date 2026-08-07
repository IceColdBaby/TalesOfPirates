using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Top.Gltf;

namespace Top.Assets.Contract.Models.Extras
{
    /// <summary>
    /// Everything the converter resolved about a material that standard glTF
    /// has no field for, carried on the material's extras under one member.
    /// Every section is optional and an absent one means the vanilla default.
    /// </summary>
    public class MaterialExtras
    {
        public const string Key = "TOP_material";

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = { new StringEnumConverter() },
            });

        [JsonProperty("renderState")] public RenderStateExtras RenderState;
        [JsonProperty("uvAnimation")] public UvAnimationExtras UvAnimation;
        [JsonProperty("opacityAnimation")] public OpacityAnimationExtras OpacityAnimation;
        [JsonProperty("flipbook")] public FlipbookExtras Flipbook;

        public void Write(GltfMaterialRef material)
        {
            material.WithExtras(new JObject { [Key] = JObject.FromObject(this, Serializer) });
        }

        public static bool TryRead(GltfMaterial material, out MaterialExtras extras)
        {
            extras = null;

            if (!(material?.Extras is JObject container) || !(container[Key] is JObject payload))
            {
                return false;
            }

            try
            {
                extras = payload.ToObject<MaterialExtras>(Serializer);
            }
            catch (JsonException)
            {
                return false;
            }

            return extras != null;
        }
    }
}
