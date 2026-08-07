using System;
using System.Collections.Generic;
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

            Assert.That(capture.Messages, Is.EqualTo(new[] { "plain" }));
            Assert.That(capture.Warnings, Is.EqualTo(new[] { "careful" }));
            Assert.That(capture.DebugMessages, Is.EqualTo(new[] { "noisy" }));
            Assert.That(capture.Errors.Count, Is.EqualTo(1));
            Assert.That(capture.Errors[0].Message, Is.EqualTo("broken"));
            Assert.That(capture.Errors[0].Exception, Is.Null);
        }

        [Test]
        public void ErrorsCarryTheirException()
        {
            var capture = new Capture();
            Log.Writer = capture;
            var failure = new InvalidOperationException("why");

            Log.Error("broken", failure);

            Assert.That(capture.Errors[0].Exception, Is.SameAs(failure));
        }

        [Test]
        public void LastInstalledWriterWins()
        {
            var first = new Capture();
            var second = new Capture();
            Log.Writer = first;
            Log.Writer = second;

            Log.Info("plain");

            Assert.That(first.Messages, Is.Empty);
            Assert.That(second.Messages, Is.EqualTo(new[] { "plain" }));
        }

        [Test]
        public void WritingWithNoWriterIsANoOp()
        {
            Log.Writer = null;

            Assert.DoesNotThrow(() =>
            {
                Log.Info("plain");
                Log.Warning("careful");
                Log.Error("broken", new Exception());
                Log.Debug("noisy");
            });
        }

        private class Capture : ILogWriter
        {
            public List<string> Messages { get; } = [];

            public List<string> Warnings { get; } = [];

            public List<(string Message, Exception Exception)> Errors { get; } = [];

            public List<string> DebugMessages { get; } = [];

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
                Errors.Add((message, exception));
            }

            public void WriteDebug(string message)
            {
                DebugMessages.Add(message);
            }
        }
    }
}
