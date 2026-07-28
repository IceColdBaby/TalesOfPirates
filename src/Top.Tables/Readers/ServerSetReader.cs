using Top.Tables.Records;

namespace Top.Tables.Readers
{
    public static class ServerSetReader
    {
        public static ServerRecord Read(TableRow row)
        {
            var record = new ServerRecord
            {
                Id = row.NextInt(),
                Name = row.NextString(),
                Region = row.NextString()
            };

            string[] ips = row.NextStrings(5);

            int validCount = 0;

            while (validCount < ips.Length && ips[validCount] != "0")
            {
                validCount++;
            }

            record.GateIps = new string[validCount];
            System.Array.Copy(ips, record.GateIps, validCount);

            return record;
        }
    }
}
