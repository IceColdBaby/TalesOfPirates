namespace Top.Tables.Records
{
    public class SceneObjectInfoRecord : TableRecord
    {
        public string DisplayName = string.Empty;
        public int Type;
        public int AttachEffectId;
        public bool EnableEnvLight;
        public bool EnablePointLight;
        public int Style;
        public int Flag;
        public int SizeFlag;
        public bool ShadeFlag;
        public bool IsReallyBig;

        // Type 0: fade
        public int[] FadeObjSeq = System.Array.Empty<int>();
        public float FadeCoefficient;

        // Type 3: point light
        public int[] PointColor = System.Array.Empty<int>();
        public int PointLightRange;
        public float PointLightAttenuation;
        public int PointLightAnimCtrlId;

        // Type 4: ambient light
        public int[] EnvColor = System.Array.Empty<int>();

        // Type 5: fog
        public int[] FogColor;

        // Type 6: env sound
        public string EnvSound = string.Empty;
        public int EnvSoundDistance;
    }
}
