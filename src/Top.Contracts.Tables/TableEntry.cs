using Newtonsoft.Json;

namespace Top.Contracts.Tables
{
    /// <summary>
    /// Base class for table entries, designed to be extended by specific table entry types.
    /// </summary>
    public class TableEntry
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Include)]
        public int Id;
    }
}
