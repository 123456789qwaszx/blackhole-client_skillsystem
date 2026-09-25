using NUnit.Framework;
using UnityEditor;
using BlackHole.Unity;

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

        [Test]
        public void SampleSkillCatalogAssetLoads()
        {
            var asset = AssetDatabase.LoadAssetAtPath<SkillCatalogAsset>(
                "Assets/BlackHole/SkillSystem/Config/SkillCatalog.asset");
            Assert.NotNull(asset);
            SkillLoadResult result = asset.Load();
            Assert.IsTrue(result.Succeeded, string.Join("\n", result.Diagnostics));
            Assert.AreEqual(2, result.Catalog.AvailableSkills.Count);
            Assert.AreEqual(2f, result.Catalog.StatsFor(SkillType.Breaker).Damage);
            Assert.AreEqual(3f, result.Catalog.StatsFor(SkillType.PiercingLaser).Damage);
        }
    }
}
