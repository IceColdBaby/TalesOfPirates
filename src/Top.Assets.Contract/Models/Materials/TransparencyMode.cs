namespace Top.Assets.Contract.Models.Materials
{
    /// <summary>
    /// The transparency variants the original client's materials pick from,
    /// each names a stock source/destination blend factor pair.
    /// </summary>
    public enum TransparencyMode
    {
        Filter,
        Additive,
        Additive1,
        Additive2,
        Additive3,
        Subtractive,
        Subtractive1,
        Subtractive2,
        Subtractive3,
    }
}
