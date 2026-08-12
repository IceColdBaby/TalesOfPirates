using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Readers
{
    public static class SceneObjectInfoReader
    {
        public static SceneObjectInfoRecord Read(TableRow row)
        {
            var record = new SceneObjectInfoRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                DisplayName = row.NextString(),
                Type = row.NextInt()
            };

            switch (record.Type)
            {
                // "fadeObjCount,seq...,fadeCoefficient"
                case 0:
                    {
                        float[] p = row.NextFloatList();
                        int count = p.Length > 0 ? (int)p[0] : 0;
                        record.FadeObjSeq = new int[count];

                        for (int i = 0; i < count; i++)
                        {
                            record.FadeObjSeq[i] = i + 1 < p.Length ? (int)p[i + 1] : 0;
                        }

                        if (count > 0 && count + 1 < p.Length)
                        {
                            record.FadeCoefficient = p[count + 1];
                        }

                        // type param 2, unused for type 0
                        row.Skip();
                        break;
                    }
                // point light: "r,g,b" then "range,attenuation,animCtrlId"
                case 3:
                    {
                        record.PointColor = row.NextIntList();

                        float[] p = row.NextFloatList();

                        if (p.Length > 0)
                        {
                            record.PointLightRange = (int)p[0];
                        }

                        if (p.Length > 1)
                        {
                            record.PointLightAttenuation = p[1];
                        }

                        if (p.Length > 2)
                        {
                            record.PointLightAnimCtrlId = (int)p[2];
                        }

                        break;
                    }
                // ambient light: "r,g,b"
                case 4:
                    record.EnvColor = row.NextIntList();
                    row.Skip();
                    break;
                case 5:
                    record.FogColor = row.NextIntList();
                    row.Skip();
                    break;
                // environment sound: name then audible distance
                case 6:
                    {
                        string[] p = row.NextStringList();
                        record.EnvSound = p.Length > 0 ? p[0] : "";
                        record.EnvSoundDistance = row.NextInt();
                        break;
                    }
                default:
                    // type params 1 and 2, unread for other types
                    row.Skip(2);
                    break;
            }

            record.AttachEffectId = row.NextInt();
            record.EnableEnvLight = row.NextBool();
            record.EnablePointLight = row.NextBool();
            record.Style = row.NextInt();
            record.Flag = row.NextInt();
            record.SizeFlag = row.NextInt();
            record.ShadeFlag = row.NextBool();
            record.IsReallyBig = row.NextBool();

            return record;
        }
    }
}
