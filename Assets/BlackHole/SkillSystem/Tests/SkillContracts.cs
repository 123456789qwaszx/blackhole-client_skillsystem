using System;
using System.Collections.Generic;
using BlackHole.Skills.TestPack;

namespace BlackHole.Skills.Tests
{
    public static class SkillContracts
    {
        public static IEnumerable<(string Name, Action Run)> All()
        {
            yield return ("Data.LoadAndLevelLookup", LoadAndLevelLookup);
            yield return ("Data.RejectBrokenAddressesAndStats", RejectBrokenAddressesAndStats);
            yield return ("Upgrade.ClickAdvancesOnlyTargetSkill", ClickAdvancesOnlyTargetSkill);
            yield return ("Upgrade.OnlyBetweenBattles", OnlyBetweenBattles);
            yield return ("Breaker.HitsAimRadiusWithLevelStats", BreakerHitsAimRadiusWithLevelStats);
            yield return ("Breaker.DisableStopsAttacksAndEnableRestarts", DisableStopsBreaker);
            yield return ("Laser.TelegraphThenHitsPath", LaserTelegraphThenHitsPath);
            yield return ("Laser.DisableDiscardsPendingShot", DisableDiscardsPendingLaser);
            yield return ("Battle.PlayersKeepIndependentLevels", PlayersKeepIndependentLevels);
        }

        private static GameplayData Data() => new GameplayData
        {
            Version = 1,
            Skills = new List<SkillData>
            {
                new SkillData { Type = "Breaker", Levels = new List<UpgradeStat>
                {
                    new UpgradeStat { Damage = 2, Interval = 1, Radius = 1 },
                    new UpgradeStat { Damage = 4, Interval = 0.5f, Radius = 2 },
                    new UpgradeStat { Damage = 6, Interval = 0.4f, Radius = 2.5f }
                } },
                new SkillData { Type = "PiercingLaser", Levels = new List<UpgradeStat>
                {
                    new UpgradeStat { Damage = 5, Interval = 3, Width = 1, TelegraphDuration = 0.4f }
                } }
            },
            StartingSkills = new List<string> { "Breaker" },
            Upgrades = new List<UpgradeNodeData>
            {
                new UpgradeNodeData { Id = "breaker-2", Skill = "Breaker", Level = 2 },
                new UpgradeNodeData { Id = "breaker-3", Skill = "Breaker", Level = 3 },
                new UpgradeNodeData { Id = "laser-unlock", Skill = "PiercingLaser", Level = 1 }
            }
        };

        private static SkillCatalog Catalog()
        {
            SkillLoadResult result = SkillCatalog.Load(Data());
            Check(result.Succeeded, "샘플 데이터는 유효해야 한다.");
            return result.Catalog;
        }

        private static void LoadAndLevelLookup()
        {
            SkillCatalog catalog = Catalog();
            Equal(3, catalog.MaxLevel(SkillType.Breaker));
            Equal(4f, catalog.StatsAt(SkillType.Breaker, 2).Damage);
            UpgradeStat copy = catalog.StatsAt(SkillType.Breaker, 2);
            copy.Damage = 100;
            Equal(4f, catalog.StatsAt(SkillType.Breaker, 2).Damage);
        }

        private static void RejectBrokenAddressesAndStats()
        {
            GameplayData data = Data();
            data.Skills[0].Levels[0].Damage = float.NaN;
            data.Skills[1].Levels[0].Radius = 2;
            data.Upgrades[1].Skill = "Ghost";
            data.StartingSkills.Add("Breaker");
            SkillLoadResult result = SkillCatalog.Load(data);
            Check(!result.Succeeded && result.Diagnostics.Count >= 4, "잘못된 수치·주소·중복을 거부한다.");
            Check(!SkillCatalog.Load(new GameplayData { Version = 2 }).Succeeded, "알 수 없는 버전을 거부한다.");
        }

        private static void ClickAdvancesOnlyTargetSkill()
        {
            var progress = new SkillProgress(Catalog());
            Equal(1, progress.Level(SkillType.Breaker));
            Equal(0, progress.Level(SkillType.PiercingLaser));
            Equal(UpgradeResult.RequiresPreviousLevel, progress.Purchase("breaker-3"));
            Equal(UpgradeResult.Purchased, progress.Purchase("laser-unlock"));
            Equal(1, progress.Level(SkillType.PiercingLaser));
            Equal(1, progress.Level(SkillType.Breaker));
            Equal(UpgradeResult.Purchased, progress.Purchase("breaker-2"));
            Equal(4f, progress.Snapshot()[SkillType.Breaker].Damage);
            Equal(UpgradeResult.AlreadyPurchased, progress.Purchase("breaker-2"));
            Equal(UpgradeResult.Purchased, progress.Purchase("breaker-3"));
            Equal(UpgradeResult.UnknownNode, progress.Purchase("missing"));
        }

        private static void OnlyBetweenBattles()
        {
            var progress = new SkillProgress(Catalog());
            var battle = new SkillBattle(progress, 1, 10, new FixedRandom(0));
            Equal(UpgradeResult.InBattle, progress.Purchase("breaker-2"));
            Equal(1, battle.Skills.Count);
            Equal(2f, progress.Snapshot()[SkillType.Breaker].Damage);
            battle.End();
            Equal(UpgradeResult.Purchased, progress.Purchase("breaker-2"));
            var next = new SkillBattle(progress, 1, 10, new FixedRandom(0));
            next.Aim = new Point2(0, 0);
            EnemyTarget target = next.AddEnemy(new Point2(0, 0), 10);
            next.Advance(0);
            Equal(6f, target.Health);
            next.End();
        }

        private static void BreakerHitsAimRadiusWithLevelStats()
        {
            var progress = new SkillProgress(Catalog());
            var battle = new SkillBattle(progress, 7, 10, new FixedRandom(0));
            battle.Aim = new Point2(0, 0);
            EnemyTarget near = battle.AddEnemy(new Point2(0.5f, 0), 10);
            EnemyTarget far = battle.AddEnemy(new Point2(3, 0), 10);
            battle.Advance(0);
            Equal(8f, near.Health);
            Equal(10f, far.Health);
            Equal(7, near.LastAttacker.Value);
            Equal(1, ((BreakerRuntime)battle.Skills[0]).LastHitCount);
            battle.End();
        }

        private static void LaserTelegraphThenHitsPath()
        {
            var progress = new SkillProgress(Catalog());
            Equal(UpgradeResult.Purchased, progress.Purchase("laser-unlock"));
            var battle = new SkillBattle(progress, 3, 10, new FixedRandom(0));
            battle.Aim = new Point2(0, 0);
            EnemyTarget onPath = battle.AddEnemy(new Point2(2, 0), 10);
            EnemyTarget outside = battle.AddEnemy(new Point2(2, 2), 10);
            battle.Advance(0);
            Equal(10f, onPath.Health);
            battle.Advance(0.4f);
            Equal(5f, onPath.Health);
            Equal(10f, outside.Health);
            Equal(3, onPath.LastAttacker.Value);
            Equal(1, ((LaserRuntime)battle.Skills[1]).FireCount);
            battle.End();
        }

        private static void DisableStopsBreaker()
        {
            var battle = new SkillBattle(new SkillProgress(Catalog()), 1, 10, new FixedRandom(0));
            battle.Aim = new Point2(0, 0);
            EnemyTarget enemy = battle.AddEnemy(new Point2(0, 0), 10);
            Check(battle.IsSkillEnabled(SkillType.Breaker), "시작 스킬은 기본으로 켜진다.");
            Check(!battle.SetSkillEnabled(SkillType.PiercingLaser, true), "미획득 스킬은 켤 수 없다.");
            Check(battle.SetSkillEnabled(SkillType.Breaker, false), "보유 스킬을 끈다.");
            battle.Advance(3);
            Equal(10f, enemy.Health);
            Check(battle.SetSkillEnabled(SkillType.Breaker, true), "보유 스킬을 다시 켠다.");
            battle.Advance(0);
            Equal(8f, enemy.Health);
            battle.End();
        }

        private static void DisableDiscardsPendingLaser()
        {
            var progress = new SkillProgress(Catalog());
            progress.Purchase("laser-unlock");
            var battle = new SkillBattle(progress, 1, 10, new FixedRandom(0));
            battle.Aim = new Point2(0, 0);
            EnemyTarget enemy = battle.AddEnemy(new Point2(2, 0), 10);
            battle.Advance(0);
            LaserRuntime laser = (LaserRuntime)battle.Skills[1];
            Equal(1, laser.PendingShots.Count);
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            laser = (LaserRuntime)battle.Skills[1];
            Equal(0, laser.PendingShots.Count);
            battle.Advance(1);
            Equal(10f, enemy.Health);
            battle.SetSkillEnabled(SkillType.PiercingLaser, true);
            battle.Advance(0);
            battle.Advance(0.4f);
            Equal(5f, enemy.Health);
            battle.End();
        }

        private static void PlayersKeepIndependentLevels()
        {
            SkillCatalog catalog = Catalog();
            var first = new SkillProgress(catalog);
            var second = new SkillProgress(catalog);
            first.Purchase("breaker-2");
            Equal(2, first.Level(SkillType.Breaker));
            Equal(1, second.Level(SkillType.Breaker));
            var one = new SkillBattle(first, 1, 10, new FixedRandom(0));
            var two = new SkillBattle(second, 2, 10, new FixedRandom(0));
            one.Aim = two.Aim = new Point2(0, 0);
            EnemyTarget a = one.AddEnemy(new Point2(0, 0), 10);
            EnemyTarget b = two.AddEnemy(new Point2(0, 0), 10);
            one.Advance(0); two.Advance(0);
            Equal(6f, a.Health); Equal(8f, b.Health);
            one.End(); two.End();
        }

        private static void Check(bool okay, string message)
        {
            if (!okay) throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception($"Expected {expected}, got {actual}");
        }
    }
}
