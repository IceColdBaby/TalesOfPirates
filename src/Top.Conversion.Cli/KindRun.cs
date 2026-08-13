using System;
using Top.Conversion.Pipeline;

namespace Top.Conversion.Cli
{
    /// <summary>
    /// Represents the processing and progress tracking for a specific kind within a conversion pipeline.
    /// Implements the <see cref="IProgress{ConversionProgress}"/> interface to report progress updates.
    /// </summary>
    internal class KindRun : IProgress<ConversionProgress>
    {
        private const int Steps = 20;

        private readonly string _kind;

        private int _step = -1;

        public KindRun(string kind)
        {
            _kind = kind;
        }

        public int Converted { get; private set; }

        public int Skipped { get; private set; }

        public int Failed { get; private set; }

        public string Trouble { get; set; }

        public int Total => Converted + Skipped + Failed;

        public string Summary
        {
            get
            {
                var counts = $"{_kind}: {Converted} converted, {Skipped} skipped, {Failed} failed";

                return Trouble == null ? counts : $"{counts}, {Trouble}";
            }
        }

        public void Report(ConversionProgress progress)
        {
            var step = progress.Count > 0 ? (progress.Index * Steps) / progress.Count : 0;

            if (step == _step)
            {
                return;
            }

            _step = step;

            Console.WriteLine($"{_kind} {progress.Index}/{progress.Count}");
        }

        public void Add(UnitResult result)
        {
            switch (result.Outcome)
            {
                case ConversionOutcome.Converted:
                    Converted++;

                    break;

                case ConversionOutcome.Skipped:
                    Skipped++;

                    break;

                default:
                    Failed++;

                    Console.WriteLine($"failed {_kind} {result.Id} '{result.Name}'");

                    break;
            }
        }
    }
}
