using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests;

public class NpcListTableTests
{
    [Test]
    public void Reads_npc_list_row()
    {
        using var stream = File.OpenRead(Fixtures.Path("tables/npclist.txt"));
        var table = TableFile.Read<NpcListRecord>(stream);

        var record = table[1];

        Assert.That(record.Name, Is.EqualTo("Argent Teleporter - Jovial"));
        Assert.That(record.Area, Is.EqualTo("Argent City"));
        Assert.That(record.X, Is.EqualTo(2187));
        Assert.That(record.Y, Is.EqualTo(2776));
        Assert.That(record.MapName, Is.EqualTo("Ascaron"));
    }
}
