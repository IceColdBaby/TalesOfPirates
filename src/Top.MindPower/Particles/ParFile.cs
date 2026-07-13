using System.IO;
using System.Text;

namespace Top.MindPower.Particles
{
    /// <summary>
    /// A .par particle-controller file.
    /// <br/> CMPPartCtrl::LoadFromFile/SaveToFile (MPParticleCtrl.cpp)
    /// </summary>
    public class ParFile
    {
        private const uint VersionMin = 2;
        private const uint VersionMax = 15;
        private const int MaxEmitters = 1024;
        private const int MaxStrips = 256;
        private const int MaxModels = 256;

        public uint Version;
        public string PartName;
        public float Length;
        public ParticleEmitter[] Emitters;
        public ParticleStrip[] Strips;
        public ParticleModel[] Models;

        public static ParFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var version = r.ReadUInt32();

            if (version < VersionMin || version > VersionMax)
            {
                throw new ParseException("par", version, r.BaseStream.Position,
                    $"unsupported version {version}");
            }

            var file = new ParFile
            {
                Version = version,
                PartName = r.ReadFixedString(ParSerialization.NameBytes),
            };

            var emitterCount = r.ReadInt32();
            if (emitterCount < 0 || emitterCount > MaxEmitters)
            {
                throw new ParseException("par", version, r.BaseStream.Position,
                    $"implausible part_num {emitterCount}");
            }

            if (version >= 3)
            {
                file.Length = r.ReadSingle();
            }

            file.Emitters = new ParticleEmitter[emitterCount];
            for (var i = 0; i < emitterCount; i++)
            {
                file.Emitters[i] = r.ReadEmitter(version);
            }

            if (version >= 7)
            {
                var stripCount = r.ReadInt32();
                if (stripCount < 0 || stripCount > MaxStrips)
                {
                    throw new ParseException("par", version, r.BaseStream.Position,
                        $"implausible strip_num {stripCount}");
                }

                file.Strips = new ParticleStrip[stripCount];
                for (var i = 0; i < stripCount; i++)
                {
                    file.Strips[i] = r.ReadStrip();
                }
            }

            if (version >= 8)
            {
                var modelCount = r.ReadInt32();
                if (modelCount < 0 || modelCount > MaxModels)
                {
                    throw new ParseException("par", version, r.BaseStream.Position,
                        $"implausible model_num {modelCount}");
                }

                file.Models = new ParticleModel[modelCount];
                for (var i = 0; i < modelCount; i++)
                {
                    file.Models[i] = r.ReadModel();
                }
            }

            return file;
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            w.Write(Version);
            w.WriteFixedString(PartName, ParSerialization.NameBytes);
            w.Write(Emitters.Length);

            if (Version >= 3)
            {
                w.Write(Length);
            }

            foreach (var emitter in Emitters)
            {
                w.WriteEmitter(emitter, Version);
            }

            if (Version >= 7)
            {
                w.Write(Strips.Length);
                foreach (var strip in Strips)
                {
                    w.WriteStrip(strip);
                }
            }

            if (Version >= 8)
            {
                w.Write(Models.Length);
                foreach (var model in Models)
                {
                    w.WriteModel(model);
                }
            }

            w.Flush();
        }
    }
}
