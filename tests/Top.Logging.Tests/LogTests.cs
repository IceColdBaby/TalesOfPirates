using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Top.Logging.Tests
{
    public class LogTests
    {
        private ILogWriter _saved;

        [SetUp]
        public void SetUp()
        {
            _saved = Log.Writer;
        }

        [TearDown]
        public void TearDown()
        {
            Log.Writer = _saved;
        }

        [Test]
        public void RoutesEachSeverityToTheWriter()
        {
            var capture = new Capture();
            Log.Writer = capture;

            Log.Info("plain");
            Log.Warning("careful");
            Log.Error("broken");
            Log.Debug("noisy");

            Assert.That(capture.Entries.Select(entry => entry.Level),
                Is.EqualTo(new[] { LogLevel.Info, LogLevel.Warning, LogLevel.Error, LogLevel.Debug }));
            Assert.That(capture.Entries.Select(entry => entry.Message),
                Is.EqualTo(new[] { "plain", "careful", "broken", "noisy" }));
            Assert.That(capture.Entries.Select(entry => entry.Exception), Is.All.Null);
        }

        [Test]
        public void ErrorsCarryTheirException()
        {
            var capture = new Capture();
            Log.Writer = capture;
            var failure = new InvalidOperationException("why");

            Log.Error("broken", failure);

            Assert.That(capture.Entries[0].Exception, Is.SameAs(failure));
        }

        [Test]
        public void WarningsCarryTheirException()
        {
            var capture = new Capture();
            Log.Writer = capture;
            var failure = new InvalidOperationException("why");

            Log.Warning("careful", failure);

            Assert.That(capture.Entries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(capture.Entries[0].Exception, Is.SameAs(failure));
        }

        [Test]
        public void SeverityOrdersFromDebugToError()
        {
            Assert.That(LogLevel.Debug, Is.LessThan(LogLevel.Info));
            Assert.That(LogLevel.Info, Is.LessThan(LogLevel.Warning));
            Assert.That(LogLevel.Warning, Is.LessThan(LogLevel.Error));
        }

        [Test]
        public void LastInstalledWriterWins()
        {
            var first = new Capture();
            var second = new Capture();
            Log.Writer = first;
            Log.Writer = second;

            Log.Info("plain");

            Assert.That(first.Entries, Is.Empty);
            Assert.That(second.Entries.Select(entry => entry.Message), Is.EqualTo(new[] { "plain" }));
        }

        [Test]
        public void WritingWithNoWriterIsANoOp()
        {
            Log.Writer = null;

            Assert.DoesNotThrow(() =>
            {
                Log.Info("plain");
                Log.Warning("careful", new Exception());
                Log.Error("broken", new Exception());
                Log.Debug("noisy");
            });
        }

        private class Capture : ILogWriter
        {
            public List<(LogLevel Level, string Message, Exception Exception)> Entries { get; } = [];

            public void Write(LogLevel level, string message, Exception exception)
            {
                Entries.Add((level, message, exception));
            }
        }
    }
}
