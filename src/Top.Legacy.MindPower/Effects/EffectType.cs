namespace Top.Legacy.MindPower.Effects
{
    /// <summary>
    /// Sub-effect rendering type stored per effect entry in an .eff file.
    /// <br/> I_Effect::LoadFromFile (I_Effect.cpp)
    /// </summary>
    public enum EffectType
    {
        None = 0,
        FrameTex = 1,
        ModelUv = 2,
        ModelTexture = 3,
        Model = 4,
    }
}
