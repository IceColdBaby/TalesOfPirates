namespace Top.Legacy.Tables.Records
{
    public class SceneEffectInfoRecord : TableRecord
    {
        public string DisplayName = string.Empty;
        public string PhotoName = string.Empty;
        public int EffectType;
        public int ObjectType;
        public int[] DummyIds = System.Array.Empty<int>();
        public int Dummy2;
        public int HeightOffset;
        public float PlayTime;
        public int LightId;
        public float BaseSize;
    }
}
