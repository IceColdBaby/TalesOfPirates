using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Top.Contracts.Assets.Models.Materials;

namespace Top.Contracts.Assets.Models.Extras
{
    /// <summary>
    /// Everything the converter resolved about a material that standard glTF
    /// has no field for, carried on the material's extras under one member.
    /// Every section is optional and an absent one means the vanilla default.
    /// </summary>
    public class MaterialExtras
    {
        public const string Key = "TOP_material";

        private static readonly JsonSerializer Serializer = JsonSerializer
            .Create(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = { new StringEnumConverter() },
            });

        [JsonProperty("renderState")] public RenderStateExtras RenderState;
        [JsonProperty("uvAnimation")] public UvAnimationExtras UvAnimation;
        [JsonProperty("opacityAnimation")] public OpacityAnimationExtras OpacityAnimation;
        [JsonProperty("flipbook")] public FlipbookExtras Flipbook;

        public static bool TryRead(JObject extras, out MaterialExtras read)
        {
            read = null;

            if (!(extras?[Key] is JObject payload))
            {
                return false;
            }

            try
            {
                read = payload.ToObject<MaterialExtras>(Serializer);
            }
            catch (JsonException)
            {
                return false;
            }

            return read != null;
        }

        public void Refine(RenderState state)
        {
            state.UvAnimated = UvAnimation != null;
            state.OpacityAnimated = OpacityAnimation != null;

            var refinement = RenderState;

            if (refinement == null)
            {
                return;
            }

            state.SrcBlend = refinement.SrcBlend;
            state.DstBlend = refinement.DstBlend;
            state.BlendEnabled = refinement.BlendEnabled;
            state.ZWrite = refinement.ZWrite;
            state.Lit = refinement.Lit;
            state.Transparency = refinement.Transparency;

            if (refinement.Cull != null)
            {
                state.Cull = refinement.Cull.Value;
            }

            if (refinement.AlphaTestCutoff != null)
            {
                state.AlphaTest = true;
                state.Cutoff = refinement.AlphaTestCutoff.Value;
            }
        }

        public JObject ToJson()
        {
            return JObject.FromObject(this, Serializer);
        }
    }
}
