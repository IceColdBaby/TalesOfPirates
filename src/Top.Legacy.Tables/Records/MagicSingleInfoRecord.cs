namespace Top.Legacy.Tables.Records
{
    public class MagicSingleInfoRecord : TableRecord
    {
        public string[] Models = System.Array.Empty<string>();
        public int Velocity;
        public string[] Particles = System.Array.Empty<string>();
        public int[] DummyIndices = System.Array.Empty<int>();
        public int MotionType;
        public int LightId;
        public string ResultParticle = string.Empty;
    }
}
