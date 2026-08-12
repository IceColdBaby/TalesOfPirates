using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Top.Logging;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// A placed world object: its sceneobjinfo row names the model file, and
    /// the row id is what a placement in a map refers to.
    /// </summary>
    public class SceneObjectResult : UnitResult
    {
        public SceneObjectResult(int id, string name, ConversionOutcome outcome, ModelArtifact model)
            : base(id, name, outcome)
        {
            Model = model;
        }

        public ModelArtifact Model { get; }

        public override IEnumerable<ModelArtifact> Artifacts =>
            Model == null ? Enumerable.Empty<ModelArtifact>() : new[] { Model };
    }

    /// <summary>
    /// Converts sceneobjinfo units. Rows sharing a model file convert once, the rest read as skipped.
    /// </summary>
    public class SceneObjectConverter
    {
        private readonly ConversionSettings _settings;
        private readonly ClientTables _tables;
        private readonly ModelConverter _models;

        public SceneObjectConverter(ConversionSettings settings, ClientTables tables, ModelConverter models)
        {
            _settings = settings;
            _tables = tables;
            _models = models;
        }

        public SceneObjectResult Convert(int id)
        {
            if (_tables.SceneObjects == null || !_tables.SceneObjects.TryGetById(id, out var row) ||
                string.IsNullOrEmpty(row.Name))
            {
                Log.Error($"no sceneobjinfo row {id} with a model file");

                return new SceneObjectResult(id, null, ConversionOutcome.Failed, null);
            }

            return Convert(row.Id, row.Name);
        }

        public IEnumerable<SceneObjectResult> ConvertAll(IProgress<ConversionProgress> progress = null,
            CancellationToken cancellation = default)
        {
            var rows = _tables.SceneObjects?
                .Where(record => !string.IsNullOrEmpty(record.Name))
                .ToList() ?? new List<SceneObjectInfoRecord>();

            return Batch.Run(rows,
                row => $"scene object {row.Id}",
                row => Convert(row.Id, row.Name),
                row => new SceneObjectResult(row.Id, row.Name, ConversionOutcome.Failed, null),
                progress, cancellation);
        }

        public int TypeIdOf(string fileName)
        {
            var row = _tables.SceneObjects?.FirstOrDefault(record =>
                string.Equals(record.Name, fileName, StringComparison.OrdinalIgnoreCase));

            return row?.Id ?? -1;
        }

        private SceneObjectResult Convert(int id, string fileName)
        {
            Log.Info($"converting scene object {id} '{fileName}'");

            var source = _settings.Source.Model("scene", fileName);

            if (!File.Exists(source))
            {
                Log.Error($"missing '{source}'");

                return new SceneObjectResult(id, fileName, ConversionOutcome.Failed, null);
            }

            var artifact = _models.Convert(source);

            return artifact == null
                ? new SceneObjectResult(id, fileName, ConversionOutcome.Failed, null)
                : new SceneObjectResult(id, fileName, artifact.Outcome, artifact);
        }
    }
}
