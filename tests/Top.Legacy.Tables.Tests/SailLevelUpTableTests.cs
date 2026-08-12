using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Records;

namespace Top.Legacy.Tables.Tests;

public class SailLevelUpTableTests
{
    [Test]
    public void Reads_sail_level_curve()
    {
        using var stream = File.OpenRead(Fixtures.Path("tables/saillvup.txt"));
        var table = TableFile.Read<SailLevelUpRecord>(stream);

        Assert.That(table[2].Exp, Is.EqualTo(625));
        Assert.That(table[10].Exp, Is.EqualTo(28561));
    }
}
