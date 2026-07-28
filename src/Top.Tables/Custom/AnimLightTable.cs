using System;
using System.Collections.Generic;
using System.IO;
using Top.Text;

namespace Top.Tables.Custom
{
    public class AnimLightKeyframe
    {
        public int Id;
        public int LightType;
        public float[] Ambient;
        public float Range;
        public float Attenuation0;
        public float Attenuation1;
        public float Attenuation2;
    }

    public class AnimLightGroup
    {
        public readonly List<AnimLightKeyframe> Keyframes = new List<AnimLightKeyframe>();
    }

    public class AnimLightTable
    {
        public readonly List<AnimLightGroup> Groups = new List<AnimLightGroup>();

        public static AnimLightTable Read(Stream stream)
        {
            var lines = Gbk.ReadLines(stream);
            var i = 0;

            string NextLine()
            {
                while (lines[i].Trim().Length == 0)
                {
                    i++;
                }

                return lines[i++].Trim();
            }

            var table = new AnimLightTable();
            var groupCount = CText.Atoi(NextLine());

            for (var g = 0; g < groupCount; g++)
            {
                var group = new AnimLightGroup();
                var keyCount = CText.Atoi(NextLine());

                for (var k = 0; k < keyCount; k++)
                {
                    var fields = NextLine().Split(',');
                    var rgb = fields[2].Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    var attenuation = fields[4].Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

                    group.Keyframes.Add(new AnimLightKeyframe
                    {
                        Id = CText.Atoi(fields[0].Trim()),
                        LightType = CText.Atoi(fields[1].Trim()),
                        Ambient = new[]
                        {
                            CText.Atof(rgb[0]) / 255f,
                            CText.Atof(rgb[1]) / 255f,
                            CText.Atof(rgb[2]) / 255f,
                        },
                        Range = CText.Atof(fields[3].Trim()),
                        Attenuation0 = CText.Atof(attenuation[0]),
                        Attenuation1 = CText.Atof(attenuation[1]),
                        Attenuation2 = CText.Atof(attenuation[2]),
                    });
                }

                table.Groups.Add(group);
            }

            return table;
        }
    }
}
