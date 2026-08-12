namespace Top.Legacy.Tables.Records
{
    public enum ResourceType
    {
        Unknown = -1,
        Particle = 0,
        Path = 1,
        Effect = 2,
        Mesh = 3,
        Texture = 4,
    }

    public class ResourceInfoRecord : TableRecord
    {
        public ResourceType Type;
    }
}
