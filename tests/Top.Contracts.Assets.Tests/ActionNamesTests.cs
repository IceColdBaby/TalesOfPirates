using NUnit.Framework;
using Top.Contracts.Assets.Models;

namespace Top.Contracts.Assets.Tests
{
    public class ActionNamesTests
    {
        [Test]
        public void Action_names_come_from_the_client_enum()
        {
            Assert.That(ActionNames.TryGetName(5, out var run), Is.True);
            Assert.That(run, Is.EqualTo("run"));
            Assert.That(ActionNames.TryGetName(42, out var fly), Is.True);
            Assert.That(fly, Is.EqualTo("fly_waiting"));
            Assert.That(ActionNames.TryGetName(60, out _), Is.False);
            Assert.That(ActionNames.TryGetName(0, out _), Is.False);
        }
    }
}
