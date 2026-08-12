using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Top.Legacy.MindPower.Tests
{
    internal static class GoldenAssert
    {
        internal static void RoundTrips<T>(string fixture, Func<Stream, T> read, Action<Stream, T> write)
        {
            RoundTripsBytes(File.ReadAllBytes(Fixtures.Path(fixture)), read, write);
        }

        internal static void RoundTripsBytes<T>(byte[] original, Func<Stream, T> read, Action<Stream, T> write)
        {
            var b1 = Canonical(original, read, write);
            var b2 = Canonical(b1, read, write);

            Assert.That(b2, Is.EqualTo(b1), "writer output is not a stable fixed point; reader/writer disagree");
        }

        internal static void RoundTripsAll<T>(
            string relativeDirectory, string searchPattern,
            Func<Stream, T> read, Action<Stream, T> write)
        {
            var directory = Path.Combine(Fixtures.FixturesPath, relativeDirectory);

            Assert.That(Directory.Exists(directory), Is.True, $"fixtures directory '{directory}' does not exist");

            var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);

            Assert.That(files.Length, Is.GreaterThan(0),
                $"no '{searchPattern}' files under '{directory}' — nothing to verify");

            var failures = new List<string>();
            var corruptFiles = new List<string>();
            var preserved = 0;
            var empty = 0;

            foreach (var file in files)
            {
                var bytes = File.ReadAllBytes(file);

                if (bytes.Length == 0)
                {
                    empty++;
                    continue;
                }

                byte[] b1;

                try
                {
                    b1 = Canonical(bytes, read, write);
                }
                catch (Exception)
                {
                    corruptFiles.Add(file);
                    continue;
                }

                try
                {
                    var b2 = Canonical(b1, read, write);
                    Assert.That(b2, Is.EqualTo(b1),
                        "writer output is not a stable fixed point; reader/writer disagree");
                    preserved++;
                }
                catch (Exception e)
                {
                    failures.Add($"{file}: {e.Message}");
                }
            }

            TestContext.WriteLine($"fidelity: preserved {preserved}/{files.Length} files ({empty} empty)");

            Assert.That(corruptFiles, Is.Empty,
                "fixtures must all parse; the reader rejected:\n"
                + string.Join("\n", corruptFiles));

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static byte[] Canonical<T>(byte[] input, Func<Stream, T> read, Action<Stream, T> write)
        {
            T model;

            using (var stream = new MemoryStream(input))
            {
                model = read(stream);
            }

            using var output = new MemoryStream();
            write(output, model);

            return output.ToArray();
        }
    }
}
