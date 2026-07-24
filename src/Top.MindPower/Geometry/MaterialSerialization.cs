using System.IO;

namespace Top.MindPower.Geometry
{
    /// <summary>
    /// The material/texture sub-block.
    /// <br/> lwMtlTexInfo::Load/Save (lwExpObj.cpp)
    /// </summary>
    internal static class MaterialSerialization
    {
        private const uint D3DRS_ALPHAREF = 24;
        private const uint D3DRS_ALPHAFUNC = 25;
        private const uint D3DCMP_GREATER = 5;
        private const int TssAtomCount = 8;

        internal static uint ReadMaterialVersion(this BinaryReader r, uint version)
        {
            return version == 0 ? r.ReadUInt32() : version;
        }

        internal static MaterialTexture[] ReadMaterials(this BinaryReader r, uint materialVersion)
        {
            var count = r.ReadUInt32();

            if (count == 0 || count > 256)
            {
                throw new ParseException("mtl", materialVersion, r.BaseStream.Position,
                    $"implausible material count {count}");
            }

            var layout = ResolveLayout(materialVersion);
            var results = new MaterialTexture[count];

            for (var i = 0; i < (int)count; i++)
            {
                var info = new MaterialTexture();

                switch (layout)
                {
                    case 0:
                        info.Opacity = 1.0f;
                        info.Transparency = TransparencyType.Filter;
                        info.Material = r.ReadMaterialDefinition();
                        info.RenderStates = r.ReadLegacyRenderStates();
                        ApplyLegacyAlphaRemap(info.RenderStates);
                        info.Stages = new TextureStage[4];
                        for (var s = 0; s < 4; s++)
                        {
                            info.Stages[s] = r.ReadTexInfoV0();
                        }

                        break;

                    case 1:
                        info.Opacity = r.ReadSingle();
                        info.Transparency = (TransparencyType)r.ReadUInt32();
                        info.Material = r.ReadMaterialDefinition();
                        info.RenderStates = r.ReadLegacyRenderStates();
                        ApplyLegacyAlphaRemap(info.RenderStates);
                        info.Stages = new TextureStage[4];
                        for (var s = 0; s < 4; s++)
                        {
                            info.Stages[s] = r.ReadTexInfoV1();
                        }

                        break;

                    default:
                        info.Opacity = r.ReadSingle();
                        info.Transparency = (TransparencyType)r.ReadUInt32();
                        info.Material = r.ReadMaterialDefinition();
                        info.RenderStates = r.ReadRenderStateAtoms();
                        info.Stages = new TextureStage[4];
                        for (var s = 0; s < 4; s++)
                        {
                            info.Stages[s] = r.ReadTexInfo();
                        }

                        break;
                }

                results[i] = info;
            }

            return results;
        }

        internal static void WriteMaterials(this BinaryWriter w, MaterialTexture[] materials,
            uint materialVersion, uint version)
        {
            if (version == 0)
            {
                w.Write(materialVersion);
            }

            w.Write((uint)materials.Length);

            var layout = ResolveLayout(materialVersion);
            foreach (var mat in materials)
            {
                switch (layout)
                {
                    case 0:
                        w.WriteMaterialDefinition(mat.Material);
                        w.WriteLegacyRenderStates(mat.RenderStates);
                        foreach (var stage in mat.Stages)
                        {
                            w.WriteTexInfoV0(stage);
                        }

                        break;

                    case 1:
                        w.Write(mat.Opacity);
                        w.Write((uint)mat.Transparency);
                        w.WriteMaterialDefinition(mat.Material);
                        w.WriteLegacyRenderStates(mat.RenderStates);
                        foreach (var stage in mat.Stages)
                        {
                            w.WriteTexInfoV1(stage);
                        }

                        break;

                    default:
                        w.Write(mat.Opacity);
                        w.Write((uint)mat.Transparency);
                        w.WriteMaterialDefinition(mat.Material);
                        w.WriteRenderStateAtoms(mat.RenderStates);
                        foreach (var stage in mat.Stages)
                        {
                            w.WriteTexInfo(stage);
                        }

                        break;
                }
            }
        }

        private static int ResolveLayout(uint materialVersion)
        {
            if (materialVersion >= 4096 || materialVersion == 2)
            {
                return 2;
            }

            return (int)materialVersion;
        }

        private static MaterialDefinition ReadMaterialDefinition(this BinaryReader r)
        {
            return new MaterialDefinition
            {
                Diffuse = r.ReadRgbaF(),
                Ambient = r.ReadRgbaF(),
                Specular = r.ReadRgbaF(),
                Emissive = r.ReadRgbaF(),
                Power = r.ReadSingle(),
            };
        }

        private static void WriteMaterialDefinition(this BinaryWriter w, MaterialDefinition m)
        {
            w.Write(m.Diffuse);
            w.Write(m.Ambient);
            w.Write(m.Specular);
            w.Write(m.Emissive);
            w.Write(m.Power);
        }

        internal static RenderStateAtom[] ReadRenderStateAtoms(this BinaryReader r)
        {
            var atoms = new RenderStateAtom[TssAtomCount];

            for (var i = 0; i < TssAtomCount; i++)
            {
                atoms[i].State = r.ReadUInt32();
                atoms[i].Value0 = r.ReadUInt32();
                atoms[i].Value1 = r.ReadUInt32();
            }

            return atoms;
        }

        internal static RenderStateAtom[] ReadLegacyRenderStates(this BinaryReader r)
        {
            var atoms = new RenderStateAtom[TssAtomCount];

            for (var i = 0; i < TssAtomCount; i++)
            {
                var state = r.ReadUInt32();
                var value = r.ReadUInt32();
                atoms[i] = new RenderStateAtom { State = state, Value0 = value, Value1 = value };
            }

            r.ReadBytes(64);
            return atoms;
        }

        /// <summary>
        /// The two oldest material layouts have their alpha func and ref
        /// replaced as they are read; the exported values never reach the
        /// engine. Applies to both, not just the oldest.
        /// <br/> lwMtlTexInfo_Load (lwExpObj.cpp)
        /// </summary>
        private static void ApplyLegacyAlphaRemap(RenderStateAtom[] atoms)
        {
            for (var i = 0; i < atoms.Length; i++)
            {
                if (atoms[i].State == D3DRS_ALPHAFUNC)
                {
                    atoms[i].Value0 = D3DCMP_GREATER;
                    atoms[i].Value1 = D3DCMP_GREATER;
                }
                else if (atoms[i].State == D3DRS_ALPHAREF)
                {
                    atoms[i].Value0 = 129;
                    atoms[i].Value1 = 129;
                }
            }
        }

        private static TextureStage ReadTexInfoV0(this BinaryReader r)
        {
            return new TextureStage
            {
                Stage = r.ReadUInt32(),
                ColorKeyType = (ColorKeyType)r.ReadUInt32(),
                ColorKey = r.ReadRgba32(),
                Format = r.ReadUInt32(),
                FileName = r.ReadFixedString(64),
                TssSet = r.ReadLegacyRenderStates(),
            };
        }

        private static TextureStage ReadTexInfoV1(this BinaryReader r)
        {
            var t = ReadTexInfoFields(r);
            r.ReadUInt32();
            t.TssSet = r.ReadLegacyRenderStates();
            return t;
        }

        internal static TextureStage ReadTexInfo(this BinaryReader r)
        {
            var t = ReadTexInfoFields(r);
            r.ReadUInt32();
            t.TssSet = r.ReadRenderStateAtoms();
            return t;
        }

        private static TextureStage ReadTexInfoFields(BinaryReader r)
        {
            return new TextureStage
            {
                Stage = r.ReadUInt32(),
                Level = r.ReadUInt32(),
                Usage = r.ReadUInt32(),
                Format = r.ReadUInt32(),
                Pool = r.ReadUInt32(),
                ByteAlignmentFlag = r.ReadUInt32(),
                Type = r.ReadUInt32(),
                Width = r.ReadUInt32(),
                Height = r.ReadUInt32(),
                ColorKeyType = (ColorKeyType)r.ReadUInt32(),
                ColorKey = r.ReadRgba32(),
                FileName = r.ReadFixedString(64),
            };
        }

        internal static void WriteRenderStateAtoms(this BinaryWriter w, RenderStateAtom[] atoms)
        {
            for (var i = 0; i < TssAtomCount; i++)
            {
                w.Write(atoms[i].State);
                w.Write(atoms[i].Value0);
                w.Write(atoms[i].Value1);
            }
        }

        internal static void WriteLegacyRenderStates(this BinaryWriter w, RenderStateAtom[] atoms)
        {
            for (var i = 0; i < TssAtomCount; i++)
            {
                w.Write(atoms[i].State);
                w.Write(atoms[i].Value0);
            }

            w.Write(new byte[64]);
        }

        private static void WriteTexInfoV0(this BinaryWriter w, TextureStage t)
        {
            w.Write(t.Stage);
            w.Write((uint)t.ColorKeyType);
            w.WriteRgba32(t.ColorKey);
            w.Write(t.Format);
            w.WriteFixedString(t.FileName, 64);
            w.WriteLegacyRenderStates(t.TssSet);
        }

        private static void WriteTexInfoV1(this BinaryWriter w, TextureStage t)
        {
            WriteTexInfoFields(w, t);
            w.Write(0u);
            w.WriteLegacyRenderStates(t.TssSet);
        }

        internal static void WriteTexInfo(this BinaryWriter w, TextureStage t)
        {
            WriteTexInfoFields(w, t);
            w.Write(0u);
            w.WriteRenderStateAtoms(t.TssSet);
        }

        private static void WriteTexInfoFields(BinaryWriter w, TextureStage t)
        {
            w.Write(t.Stage);
            w.Write(t.Level);
            w.Write(t.Usage);
            w.Write(t.Format);
            w.Write(t.Pool);
            w.Write(t.ByteAlignmentFlag);
            w.Write(t.Type);
            w.Write(t.Width);
            w.Write(t.Height);
            w.Write((uint)t.ColorKeyType);
            w.WriteRgba32(t.ColorKey);
            w.WriteFixedString(t.FileName, 64);
        }
    }
}
