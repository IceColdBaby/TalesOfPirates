using System;
using System.Collections.Generic;
using System.Threading;
using Top.Logging;

namespace Top.Assets.Conversion.Pipeline
{
    /// <summary>
    /// Runs a converter over a list of units, reporting each before it runs.
    /// A unit that throws is logged and replaced by the caller's onFailure result,
    /// so one failure never stops the batch. Cancellation is honored between units.
    /// </summary>
    public static class Batch
    {
        public static IEnumerable<TResult> Run<TUnit, TResult>(IReadOnlyList<TUnit> units,
            Func<TUnit, string> label, Func<TUnit, TResult> convert, Func<TUnit, TResult> onFailure,
            IProgress<ConversionProgress> progress, CancellationToken cancellation)
        {
            for (var i = 0; i < units.Count; i++)
            {
                if (cancellation.IsCancellationRequested)
                {
                    yield break;
                }

                var name = label(units[i]);

                progress?.Report(new ConversionProgress(name, i, units.Count));

                TResult result;

                try
                {
                    result = convert(units[i]);
                }
                catch (Exception exception)
                {
                    Log.Error($"{name} failed", exception);

                    result = onFailure(units[i]);
                }

                yield return result;
            }
        }
    }
}
