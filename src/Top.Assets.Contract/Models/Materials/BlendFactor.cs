namespace Top.Assets.Contract.Models.Materials
{
    /// <summary>
    /// The factor a color is multiplied by in a blend stage; the values mirror
    /// the D3DBLEND_* legacy render state.
    /// </summary>
    public enum BlendFactor
    {
        Zero,
        One,
        SrcColor,
        OneMinusSrcColor,
        SrcAlpha,
        OneMinusSrcAlpha,
        DstAlpha,
        OneMinusDstAlpha,
        DstColor,
        OneMinusDstColor,
    }
}
