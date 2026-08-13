using System;
using System.Collections.Generic;

namespace Top.Conversion.Pipeline
{
    public enum ConversionOutcome
    {
        Converted,
        Skipped,
        Failed,
    }

    /// <summary>
    /// One glTF the pipeline produced and the PNGs it points at in the
    /// texture folder its kind shares.
    /// </summary>
    public class ModelArtifact
    {
        public ModelArtifact(string name, string kind, string modelPath,
            IReadOnlyList<string> texturePaths, ConversionOutcome outcome)
        {
            Name = name;
            Kind = kind;
            ModelPath = modelPath;
            TexturePaths = texturePaths;
            Outcome = outcome;
        }

        public string Name { get; }

        public string Kind { get; }

        public string ModelPath { get; }

        public IReadOnlyList<string> TexturePaths { get; }

        public ConversionOutcome Outcome { get; }

        public ModelArtifact AsSkipped()
        {
            return new ModelArtifact(Name, Kind, ModelPath, Array.Empty<string>(), ConversionOutcome.Skipped);
        }
    }
}
