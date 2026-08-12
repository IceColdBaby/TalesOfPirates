using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests;

public class JobEquipTableTests
{
    [Test]
    public void Reads_initial_job_equipment()
    {
        using var stream = File.OpenRead(Fixtures.Path("tables/Int_Cha_Item.txt"));
        var table = TableFile.Read<JobEquipRecord>(stream);

        Assert.That(table[0].Name, Is.EqualTo("Newbie"));
        Assert.That(table[0].ItemIds, Is.EqualTo(new[] { 289, 641, 8, 436 }));
    }
}
