using System;
using System.Collections.Generic;
using Top.Conversion.Pipeline;

namespace Top.Conversion.Tests.Pipeline
{
    internal class RecordedProgress : IProgress<ConversionProgress>
    {
        internal List<ConversionProgress> Steps { get; } = [];

        public void Report(ConversionProgress value)
        {
            Steps.Add(value);
        }
    }
}
