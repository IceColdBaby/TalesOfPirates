namespace Top.Assets.Conversion.Pipeline
{
    /// <summary>
    /// The content families the client's model folders map to. Conversion
    /// output is laid out under these names.
    /// </summary>
    public static class ContentKind
    {
        public const string Character = "Character";

        public const string Item = "Item";

        public const string Scene = "Scene";

        public static string Of(string folder)
        {
            return char.ToUpperInvariant(folder[0]) + folder.Substring(1).ToLowerInvariant();
        }
    }
}
