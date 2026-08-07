using System;
using System.Collections.Generic;

namespace Top.Assets.Conversion.Pipeline
{
    /// <summary>
    /// Remembers what each source file has already converted to. Lookups return
    /// the same artifact marked skipped, so a shared rig or model builds once
    /// however many units name it.
    /// </summary>
    public class ArtifactMemo
    {
        private readonly Dictionary<string, ModelArtifact> _built =
            new Dictionary<string, ModelArtifact>(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string source, out ModelArtifact artifact)
        {
            if (!_built.TryGetValue(source, out var built))
            {
                artifact = null;

                return false;
            }

            artifact = built.AsSkipped();

            return true;
        }

        public ModelArtifact Add(string source, ModelArtifact artifact)
        {
            _built[source] = artifact;

            return artifact;
        }
    }
}
