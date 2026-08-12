using System;
using System.Collections.Generic;
using Top.Logging;

namespace Top.Conversion.Tests
{
    /// <summary>
    /// Installs itself as the log writer for the lifetime of a test and keeps
    /// what was written, so a test can assert on diagnostics the pipeline
    /// reports rather than returns.
    /// </summary>
    internal class CaptureLog : ILogWriter, IDisposable
    {
        private readonly ILogWriter _previous;

        public CaptureLog()
        {
            _previous = Log.Writer;
            Log.Writer = this;
        }

        public List<string> Messages { get; } = [];

        public List<string> Warnings { get; } = [];

        public List<string> Errors { get; } = [];

        public void Write(string message)
        {
            Messages.Add(message);
        }

        public void WriteWarning(string message)
        {
            Warnings.Add(message);
        }

        public void WriteError(string message, Exception exception)
        {
            Errors.Add(exception == null ? message : $"{message}: {exception.Message}");
        }

        public void WriteDebug(string message)
        {
            Messages.Add(message);
        }

        public void Dispose()
        {
            Log.Writer = _previous;
        }
    }
}
