using System.IO;
using System.Text;
using NUnit.Framework;
using Top.MindPower.Geometry;

namespace Top.MindPower.Tests
{
    public class MaterialSerializationTests
    {
        private const uint AlphaRefState = 24;
        private const uint AlphaFuncState = 25;
        private const int LegacyAtomCount = 8;

        [TestCase(0u)]
        [TestCase(1u)]
        public void Legacy_layouts_replace_the_alpha_func_and_ref(uint materialVersion)
        {
            var material = new MaterialTexture
            {
                Opacity = 1f,
                Transparency = TransparencyType.Filter,
                RenderStates = Atoms(
                    new RenderStateAtom { State = AlphaFuncState, Value0 = 7, Value1 = 7 },
                    new RenderStateAtom { State = AlphaRefState, Value0 = 200, Value1 = 200 }),
                Stages = [Stage(), Stage(), Stage(), Stage()],
            };

            var read = RoundTrip(material, materialVersion);

            Assert.That(Value(read, AlphaFuncState), Is.EqualTo(5), "D3DCMP_GREATER");
            Assert.That(Value(read, AlphaRefState), Is.EqualTo(129), "the ref the engine applies");
        }

        [Test]
        public void Current_layouts_keep_the_exported_alpha_func_and_ref()
        {
            var material = new MaterialTexture
            {
                Opacity = 1f,
                Transparency = TransparencyType.Filter,
                RenderStates = Atoms(
                    new RenderStateAtom { State = AlphaFuncState, Value0 = 7, Value1 = 7 },
                    new RenderStateAtom { State = AlphaRefState, Value0 = 200, Value1 = 200 }),
                Stages = [Stage(), Stage(), Stage(), Stage()],
            };

            var read = RoundTrip(material, 0x1005);

            Assert.That(Value(read, AlphaFuncState), Is.EqualTo(7));
            Assert.That(Value(read, AlphaRefState), Is.EqualTo(200));
        }

        private static MaterialTexture RoundTrip(MaterialTexture material, uint materialVersion)
        {
            var stream = new MemoryStream();

            using (var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                w.WriteMaterials([material], materialVersion, version: 1);
            }

            stream.Position = 0;

            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            return r.ReadMaterials(materialVersion)[0];
        }

        private static uint Value(MaterialTexture material, uint state)
        {
            foreach (var atom in material.RenderStates)
            {
                if (atom.State == state)
                {
                    return atom.Value0;
                }
            }

            return uint.MaxValue;
        }

        private static RenderStateAtom[] Atoms(params RenderStateAtom[] used)
        {
            var atoms = new RenderStateAtom[LegacyAtomCount];
            used.CopyTo(atoms, 0);

            for (var i = used.Length; i < atoms.Length; i++)
            {
                atoms[i] = new RenderStateAtom { State = uint.MaxValue };
            }

            return atoms;
        }

        private static TextureStage Stage()
        {
            return new TextureStage { FileName = string.Empty, TssSet = Atoms() };
        }
    }
}
