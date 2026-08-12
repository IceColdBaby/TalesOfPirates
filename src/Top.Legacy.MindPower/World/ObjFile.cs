using System.IO;
using System.Text;

namespace Top.Legacy.MindPower.World
{
    /// <summary>
    /// A .obj scene-object file.
    /// <br/> CSceneObjData (ObjectData.h)
    /// </summary>
    public class ObjFile
    {
        private const string Magic = "HF Object File!";
        private const int TitleBytes = 16;
        private const int HeaderBytes = 44;
        private const int IndexEntryBytes = 8;
        private const int MaxSectionObjects = 25;
        private const int Version600 = 600;

        public int Version;
        public int SectionCountX;
        public int SectionCountY;
        public int SectionWidth;
        public int SectionHeight;
        public int SectionObjectCount;
        public ObjSection[] Sections;

        public static ObjFile Read(Stream stream)
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            var title = r.ReadFixedString(TitleBytes);

            if (title != Magic)
            {
                throw new ParseException("obj", 0, r.BaseStream.Position,
                    $"bad magic '{title}'");
            }

            var version = r.ReadInt32();
            if (version != Version600)
            {
                throw new ParseException("obj", (uint)version, r.BaseStream.Position,
                    $"unsupported version {version}");
            }

            r.ReadInt32();

            var file = new ObjFile
            {
                Version = version,
                SectionCountX = r.ReadInt32(),
                SectionCountY = r.ReadInt32(),
                SectionWidth = r.ReadInt32(),
                SectionHeight = r.ReadInt32(),
                SectionObjectCount = r.ReadInt32(),
            };

            if (file.SectionCountX < 0 || file.SectionCountY < 0)
            {
                throw new ParseException("obj", (uint)version, r.BaseStream.Position,
                    $"implausible section grid {file.SectionCountX}x{file.SectionCountY}");
            }

            var total = file.SectionCountX * file.SectionCountY;
            file.Sections = new ObjSection[total];

            for (var i = 0; i < total; i++)
            {
                r.BaseStream.Seek(HeaderBytes + (long)i * IndexEntryBytes, SeekOrigin.Begin);
                var dataOffset = r.ReadInt32();
                var count = r.ReadInt32();

                if (dataOffset == 0 || count <= 0)
                {
                    continue;
                }

                if (count > MaxSectionObjects)
                {
                    throw new ParseException("obj", (uint)version, dataOffset,
                        $"section {i} object count {count} exceeds {MaxSectionObjects}");
                }

                if (dataOffset < 0
                    || (long)dataOffset + (long)count * SceneObjectSerialization.ObjectBytes > r.BaseStream.Length)
                {
                    throw new ParseException("obj", (uint)version, dataOffset,
                        $"section {i} data out of bounds (offset {dataOffset} count {count})");
                }

                r.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
                var objects = new SceneObject[count];
                for (var o = 0; o < count; o++)
                {
                    objects[o] = r.ReadSceneObject();
                }

                file.Sections[i] = new ObjSection { Objects = objects };
            }

            return file;
        }

        public void Write(Stream stream)
        {
            using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            var total = Sections.Length;
            var slabBytes = MaxSectionObjects * SceneObjectSerialization.ObjectBytes;
            var indexBytes = total * IndexEntryBytes;
            var dataStart = HeaderBytes + indexBytes;

            var present = 0;
            for (var i = 0; i < total; i++)
            {
                if (Sections[i] != null)
                {
                    present++;
                }
            }

            var fileSize = dataStart + present * slabBytes;

            w.WriteFixedString(Magic, TitleBytes);
            w.Write(Version);
            w.Write(fileSize);
            w.Write(SectionCountX);
            w.Write(SectionCountY);
            w.Write(SectionWidth);
            w.Write(SectionHeight);
            w.Write(SectionObjectCount);

            var dataPosition = dataStart;
            for (var i = 0; i < total; i++)
            {
                if (Sections[i] == null)
                {
                    w.Write(0);
                    w.Write(0);
                    continue;
                }

                w.Write(dataPosition);
                w.Write(Sections[i].Objects.Length);
                dataPosition += slabBytes;
            }

            for (var i = 0; i < total; i++)
            {
                if (Sections[i] == null)
                {
                    continue;
                }

                var objects = Sections[i].Objects;
                foreach (var obj in objects)
                {
                    w.WriteSceneObject(obj);
                }

                var pad = (MaxSectionObjects - objects.Length) * SceneObjectSerialization.ObjectBytes;
                for (var p = 0; p < pad; p++)
                {
                    w.Write((byte)0);
                }
            }

            w.Flush();
        }
    }
}
