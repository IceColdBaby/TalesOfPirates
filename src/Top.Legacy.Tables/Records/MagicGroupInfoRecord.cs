namespace Top.Legacy.Tables.Records
{
    public class MagicGroupInfoRecord : TableRecord
    {
        public int[] EffectIds = System.Array.Empty<int>();
        public int[] Counts = System.Array.Empty<int>();
        public int TotalCount;
        public int EmissionType;
    }
}
