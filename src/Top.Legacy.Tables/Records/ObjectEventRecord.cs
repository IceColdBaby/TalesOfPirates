namespace Top.Legacy.Tables.Records
{
    public enum EventType
    {
        Action = 1,
        Entity = 2,
    }

    public enum EventArouseType
    {
        Distance = 0,
        Click = 1,
    }

    public class ObjectEventRecord : TableRecord
    {
        public EventType EventType;
        public EventArouseType ArouseType;
        public int ArouseRadius;
        public int Effect;
        public int Music;
        public int BornEffect;
        public int Cursor;
        public int MainCharacterType;
    }
}
