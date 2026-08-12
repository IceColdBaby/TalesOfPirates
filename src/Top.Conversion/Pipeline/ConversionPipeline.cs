namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// One run's converters, wired together. What they already converted is
    /// remembered for the life of the pipeline, so a batch builds each shared
    /// rig, module and merged monster once however many units name it.
    /// </summary>
    public class ConversionPipeline
    {
        public ConversionPipeline(ConversionSettings settings)
            : this(settings, new ClientTables(settings))
        {
        }

        public ConversionPipeline(ConversionSettings settings, ClientTables tables)
        {
            Settings = settings;
            Tables = tables;
            Models = new ModelConverter(settings);
            Rigs = new RigConverter(settings, tables);
            Items = new ItemConverter(settings, tables, Models);
            SceneObjects = new SceneObjectConverter(settings, tables, Models);
            Characters = new CharacterConverter(settings, tables, Rigs, Items);
        }

        public ConversionSettings Settings { get; }

        public ClientTables Tables { get; }

        public ModelConverter Models { get; }

        public RigConverter Rigs { get; }

        public ItemConverter Items { get; }

        public SceneObjectConverter SceneObjects { get; }

        public CharacterConverter Characters { get; }
    }
}
