using System.Numerics;

namespace Top.Tables.Records
{
    public class MapInfoRecord : TableRecord
    {
        public string DisplayName;
        public bool ShowSwitch;
        public int InitX;
        public int InitY;
        public Vector3 LightDirection;
        public Vector3 LightColor;
    }
}
