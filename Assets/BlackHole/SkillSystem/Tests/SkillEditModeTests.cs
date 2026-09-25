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
            Assert.AreEqual(0f, asset.LoadPlayerCombat().CritChance);
            Assert.AreEqual(2f, asset.LoadPlayerCombat().CritMultiplier);
        }

        [Test]
        public void SampleEnemyCatalogAssetLoads()
        {
            var asset = AssetDatabase.LoadAssetAtPath<EnemyCatalogAsset>(
                "Assets/BlackHole/SkillSystem/Config/EnemyCatalog.asset");
            Assert.NotNull(asset);
            EnemyCatalog catalog = asset.Load();
            Assert.AreEqual(DeathEffectType.ChainLightning, catalog.Get("electric").DeathEffect.Type);
            Assert.AreEqual(DeathEffectType.Explosion, catalog.Get("explosive").DeathEffect.Type);
            Assert.AreEqual(DeathEffectType.AttackHaste, catalog.Get("haste").DeathEffect.Type);
            Assert.AreEqual(DeathEffectType.GuaranteedCritical, catalog.Get("critical").DeathEffect.Type);
        }
    }
}
