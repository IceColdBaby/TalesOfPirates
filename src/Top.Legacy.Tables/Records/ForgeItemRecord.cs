namespace Top.Legacy.Tables.Records
{
    public class ForgeItemRecord : TableRecord
    {
        public int Level;
        public int FailedLevel;
        public int SuccessRate;
        public ForgeItemEntry[] Items;
        public int Money;
    }

    public struct ForgeItemEntry
    {
        public int Id;
        public int Count;
    }
}
