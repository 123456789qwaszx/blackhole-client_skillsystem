using NUnit.Framework;

namespace BlackHole.Skills.Tests
{
    public sealed class SkillEditModeTests
    {
        [Test]
        public void AllContracts()
        {
            foreach (var contract in SkillContracts.All())
                Assert.DoesNotThrow(() => contract.Run(), contract.Name);
        }
    }
}
