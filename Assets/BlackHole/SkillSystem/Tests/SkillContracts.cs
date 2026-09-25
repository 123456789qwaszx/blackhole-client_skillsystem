using System;
using System.Collections.Generic;
using BlackHole.Skills.TestPack;

namespace BlackHole.Skills.Tests
{
    public static class SkillContracts
    {
        public static IEnumerable<(string Name, Action Run)> All()
        {
            yield return ("Data.SkillStatsAreValidatedAndCopied", SkillStatsAreValidatedAndCopied);
            yield return ("Data.RejectsInvalidSkills", RejectsInvalidSkills);
            yield return ("Breaker.AttacksInsideAimRadius", BreakerAttacksInsideAimRadius);
            yield return ("Laser.TelegraphsThenPierces", LaserTelegraphsThenPierces);
            yield return ("Toggle.BreakerCanStopAndRestart", BreakerCanStopAndRestart);
            yield return ("Toggle.LaserDiscardsPendingShot", LaserDiscardsPendingShot);
            yield return ("Battle.PlayersRunIndependently", PlayersRunIndependently);
        }

        private static List<SkillData> Data() => new List<SkillData>
        {
            new SkillData { Type = SkillType.Breaker, Stats = new SkillStats { Damage = 2, Interval = 1, Radius = 1 } },
            new SkillData { Type = SkillType.PiercingLaser, Stats = new SkillStats
                { Damage = 5, Interval = 3, Width = 1, TelegraphDuration = 0.4f } }
        };

        private static SkillCatalog Catalog()
        {
            SkillLoadResult result = SkillCatalog.Load(Data());
            Check(result.Succeeded, "샘플 스킬 데이터는 유효해야 한다.");
            return result.Catalog;
        }

        private static void SkillStatsAreValidatedAndCopied()
        {
            SkillCatalog catalog = Catalog();
            Equal(2, catalog.AvailableSkills.Count);
            SkillStats changed = catalog.StatsFor(SkillType.Breaker);
            changed.Damage = 100;
            Equal(2f, catalog.StatsFor(SkillType.Breaker).Damage);
        }

        private static void RejectsInvalidSkills()
        {
            List<SkillData> data = Data();
            data[0].Stats.Damage = float.NaN;
            data[1].Stats.Radius = 2;
            data.Add(new SkillData { Type = (SkillType)999, Stats = new SkillStats() });
            Check(!SkillCatalog.Load(data).Succeeded, "잘못된 종류·수치를 거부한다.");
            Check(!SkillCatalog.Load(null).Succeeded, "누락된 스킬 목록을 거부한다.");
            data = Data();
            data.Add(data[0]);
            Check(!SkillCatalog.Load(data).Succeeded, "중복 Skill을 거부한다.");
        }

        private static void BreakerAttacksInsideAimRadius()
        {
            var battle = new SkillBattle(Catalog(), 7, 10, new FixedRandom(0));
            battle.Aim = new Point2(0, 0);
            EnemyTarget near = battle.AddEnemy(new Point2(0.5f, 0), 10);
            EnemyTarget far = battle.AddEnemy(new Point2(3, 2), 10);
            battle.Advance(0);
            Equal(8f, near.Health);
            Equal(10f, far.Health);
            Equal(7, near.LastAttacker.Value);
            Equal(1, ((BreakerRuntime)battle.Skills[0]).LastHitCount);
            battle.End();
        }

        private static void LaserTelegraphsThenPierces()
        {
            var battle = new SkillBattle(Catalog(), 3, 10, new FixedRandom(0));
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

        private static void BreakerCanStopAndRestart()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            battle.Aim = new Point2(0, 0);
            EnemyTarget enemy = battle.AddEnemy(new Point2(0, 0), 10);
            Check(battle.IsSkillEnabled(SkillType.Breaker), "기본으로 켜져야 한다.");
            Check(battle.SetSkillEnabled(SkillType.Breaker, false), "스킬을 끈다.");
            battle.Advance(3);
            Equal(10f, enemy.Health);
            Check(battle.SetSkillEnabled(SkillType.Breaker, true), "다시 켠다.");
            battle.Advance(0);
            Equal(8f, enemy.Health);
            battle.End();
        }

        private static void LaserDiscardsPendingShot()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.Aim = new Point2(0, 0);
            EnemyTarget enemy = battle.AddEnemy(new Point2(2, 0), 10);
            battle.Advance(0);
            Equal(1, ((LaserRuntime)battle.Skills[1]).PendingShots.Count);
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            Equal(0, ((LaserRuntime)battle.Skills[1]).PendingShots.Count);
            battle.Advance(1);
            Equal(10f, enemy.Health);
            battle.SetSkillEnabled(SkillType.PiercingLaser, true);
            battle.Advance(0);
            battle.Advance(0.4f);
            Equal(5f, enemy.Health);
            battle.End();
        }

        private static void PlayersRunIndependently()
        {
            SkillCatalog catalog = Catalog();
            var one = new SkillBattle(catalog, 1, 10, new FixedRandom(0));
            var two = new SkillBattle(catalog, 2, 10, new FixedRandom(0));
            one.Aim = two.Aim = new Point2(0, 0);
            one.SetSkillEnabled(SkillType.Breaker, false);
            EnemyTarget a = one.AddEnemy(new Point2(0, 0), 10);
            EnemyTarget b = two.AddEnemy(new Point2(0, 0), 10);
            one.Advance(0); two.Advance(0);
            Equal(10f, a.Health); Equal(8f, b.Health);
            Equal(2, b.LastAttacker.Value);
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
