namespace Top.Assets.Conversion.Pipeline
{
    /// <summary>
    /// Where the original client's files are, where converted output lands,
    /// and whether a run replaces what is already there.
    /// </summary>
    public class ConversionSettings
    {
        public ConversionSettings(string clientRoot, string outputRoot, bool overwrite = false)
        {
            ClientRoot = clientRoot;
            OutputRoot = outputRoot;
            Overwrite = overwrite;
            Source = new SourcePaths(clientRoot);
            Output = new OutputPaths(outputRoot);
        }

        public string ClientRoot { get; }

        public string OutputRoot { get; }

        public bool Overwrite { get; }

        public SourcePaths Source { get; }

        public OutputPaths Output { get; }
    }
}
