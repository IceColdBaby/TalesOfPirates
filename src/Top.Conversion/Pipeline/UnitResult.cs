using System.Collections.Generic;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// What converting one unit came to.
    /// </summary>
    public abstract class UnitResult
    {
        protected UnitResult(int id, string name, ConversionOutcome outcome)
        {
            Id = id;
            Name = name;
            Outcome = outcome;
        }

        public int Id { get; }

        public string Name { get; }

        public ConversionOutcome Outcome { get; }

        public abstract IEnumerable<ModelArtifact> Artifacts { get; }
    }
}
