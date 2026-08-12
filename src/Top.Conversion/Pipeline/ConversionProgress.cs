namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The unit a batch is about to convert and how far along it is.
    /// </summary>
    public readonly struct ConversionProgress
    {
        public ConversionProgress(string unit, int index, int count)
        {
            Unit = unit;
            Index = index;
            Count = count;
        }

        public string Unit { get; }

        public int Index { get; }

        public int Count { get; }

        public float Fraction => Count > 0 ? (float)Index / Count : 0f;
    }
}
