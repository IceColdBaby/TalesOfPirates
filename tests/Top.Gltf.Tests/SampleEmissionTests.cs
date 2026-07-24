using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Top.Gltf.Tests
{
    [Category("EmitSamples")]
    public class SampleEmissionTests
    {
        private static string ArtifactsDirectory => typeof(SampleEmissionTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(a => a.Key == "ArtifactsPath").Value;

        private static string SamplesDirectory => Path.Combine(ArtifactsDirectory, "gltf-samples");

        [Test]
        public void Emits_validator_samples()
        {
            Directory.CreateDirectory(SamplesDirectory);

            (GltfDocument doc, byte[] bin) = WriterTests.BuildTriangle();

            using (var fs = File.Create(Path.Combine(SamplesDirectory, "triangle.glb")))
            {
                GltfWriter.WriteGlb(doc, bin, fs);
            }

            (doc, bin) = WriterTests.BuildTriangle();

            using (var fs = File.Create(
                       Path.Combine(SamplesDirectory, "triangle-embedded.gltf")))
            {
                GltfWriter.WriteGltfEmbedded(doc, bin, fs);
            }

            (doc, bin) = WriterTests.BuildTriangle();

            using (var json = File.Create(Path.Combine(SamplesDirectory, "triangle.gltf")))
            using (var binStream = File.Create(Path.Combine(SamplesDirectory, "triangle.bin")))
            {
                GltfWriter.WriteGltfWithBin(doc, bin, "triangle.bin", json, binStream);
            }

            Assert.That(File.Exists(Path.Combine(SamplesDirectory, "triangle.glb")));
            Assert.That(File.Exists(Path.Combine(SamplesDirectory, "triangle-embedded.gltf")));
            Assert.That(File.Exists(Path.Combine(SamplesDirectory, "triangle.gltf")));
            Assert.That(File.Exists(Path.Combine(SamplesDirectory, "triangle.bin")));
        }
    }
}
