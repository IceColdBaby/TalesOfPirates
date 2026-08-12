using System.ComponentModel;
using Newtonsoft.Json;
using Top.Contracts.Assets.Models.Materials;

namespace Top.Contracts.Assets.Models.Extras
{
    /// <summary>
    /// Provides extensible render state properties not supported by standard glTF,
    /// allowing customization for advanced rendering scenarios.
    /// </summary>
    public class RenderStateExtras
    {
        [JsonProperty("srcBlend")] [DefaultValue(BlendFactor.One)]
        public BlendFactor SrcBlend = BlendFactor.One;

        [JsonProperty("dstBlend")] [DefaultValue(BlendFactor.Zero)]
        public BlendFactor DstBlend = BlendFactor.Zero;

        [JsonProperty("blendEnabled")] [DefaultValue(false)]
        public bool BlendEnabled;

        [JsonProperty("zWrite")] [DefaultValue(true)]
        public bool ZWrite = true;

        [JsonProperty("cull")] public FaceCulling? Cull;

        [JsonProperty("alphaTestCutoff")] public float? AlphaTestCutoff;

        [JsonProperty("lit")] [DefaultValue(true)]
        public bool Lit = true;

        [JsonProperty("transparency")] [DefaultValue(TransparencyMode.Filter)]
        public TransparencyMode Transparency = TransparencyMode.Filter;
    }
}
