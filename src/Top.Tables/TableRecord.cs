namespace Top.Tables
{
    /// <summary>
    /// Base of every row-table record: the two structural columns all
    /// RawDataSet tables share (column 0 = id, column 1 = name).
    /// </summary>
    public abstract class TableRecord
    {
        public int Id;
        public string Name;
    }
}
