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
            yield return ("Enemy.RejectsInvalidEffectData", RejectsInvalidEffectData);
            yield return ("Death.ExplosionDoesNotDamageEffectOwners", ExplosionDoesNotDamageEffectOwners);
            yield return ("Death.SkillKillTriggersEffectInSameStep", SkillKillTriggersEffectInSameStep);
            yield return ("Death.ChainStopsAtHopLimitAndDoesNotRepeat", ChainStopsAtHopLimitAndDoesNotRepeat);
            yield return ("Buff.HasteAndCriticalApplyToKillerThenExpire", HasteAndCriticalApplyToKillerThenExpire);
            yield return ("Buff.PlayersDoNotShareKillBuffs", PlayersDoNotShareKillBuffs);
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

        private static EnemyDefinition Enemy(string id, float health, DeathEffectDefinition effect = null) =>
            new EnemyDefinition { Id = id, Health = health, DeathEffect = effect ?? new DeathEffectDefinition() };

        private static void RejectsInvalidEffectData()
        {
            var source = new List<EnemyDefinition> { Enemy("normal", 10) };
            EnemyCatalog catalog = EnemyCatalog.Load(source);
            source[0].Health = -1;
            Equal(10f, catalog.Get("normal").Health);
            source[0] = Enemy("electric", 5, new DeathEffectDefinition
                { Type = DeathEffectType.ChainLightning, Damage = 3, Radius = 2, MaxTargets = 0 });
            CheckThrows(() => EnemyCatalog.Load(source), "연쇄 횟수 없는 효과를 거부한다.");
            source[0].DeathEffect.MaxTargets = 3;
            catalog = EnemyCatalog.Load(source);
            catalog.Get("electric").DeathEffect.Damage = -1;
            Equal(3f, catalog.Get("electric").DeathEffect.Damage);
            source.Add(source[0]);
            CheckThrows(() => EnemyCatalog.Load(source), "중복 ID를 거부한다.");
        }

        private static void ExplosionDoesNotDamageEffectOwners()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.SetSkillEnabled(SkillType.Breaker, false);
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            EnemyTarget explosion = battle.AddEnemy(new Point2(0, 0), Enemy("explosive", 2,
                new DeathEffectDefinition { Type = DeathEffectType.Explosion, Damage = 5, Radius = 2 }));
            EnemyTarget normal = battle.AddEnemy(new Point2(1, 0), 4);
            EnemyTarget immune = battle.AddEnemy(new Point2(1, 1), Enemy("critical", 1,
                new DeathEffectDefinition { Type = DeathEffectType.GuaranteedCritical,
                    Duration = 4, CriticalMultiplier = 2 }));
            EnemyTarget far = battle.AddEnemy(new Point2(3, 0), 4);
            explosion.Hit(2, 7);
            battle.Advance(0);
            Equal(0f, normal.Health);
            Equal(7, normal.LastAttacker.Value);
            Equal(1f, immune.Health);
            Equal(4f, far.Health);
            Equal(1, battle.LastEffectHits.Count);
            Equal(0f, battle.Buffs.CriticalRemaining(7));
            battle.Advance(0);
            Equal(0, battle.LastEffectHits.Count);
            battle.End();
        }

        private static void ChainStopsAtHopLimitAndDoesNotRepeat()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.SetSkillEnabled(SkillType.Breaker, false);
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            EnemyTarget electric = battle.AddEnemy(new Point2(0, 0), Enemy("electric", 1,
                new DeathEffectDefinition { Type = DeathEffectType.ChainLightning,
                    Damage = 2, Radius = 2, MaxTargets = 2 }));
            EnemyTarget a = battle.AddEnemy(new Point2(1, 0), 2);
            EnemyTarget b = battle.AddEnemy(new Point2(2, 0), 2);
            EnemyTarget c = battle.AddEnemy(new Point2(3, 0), 2);
            EnemyTarget immune = battle.AddEnemy(new Point2(0.5f, 0), Enemy("explosive", 1,
                new DeathEffectDefinition { Type = DeathEffectType.Explosion, Damage = 2, Radius = 2 }));
            electric.Hit(1, 3);
            battle.Advance(0);
            Equal(0f, a.Health);
            Equal(0f, b.Health);
            Equal(2f, c.Health);
            Equal(1f, immune.Health);
            Equal(2, battle.LastEffectHits.Count);
            battle.Advance(0);
            Equal(0, battle.LastEffectHits.Count);
            battle.End();
        }

        private static void SkillKillTriggersEffectInSameStep()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            battle.Aim = new Point2(0, 0);
            EnemyTarget source = battle.AddEnemy(new Point2(0, 0), Enemy("explosive", 2,
                new DeathEffectDefinition { Type = DeathEffectType.Explosion, Damage = 5, Radius = 3 }));
            EnemyTarget victim = battle.AddEnemy(new Point2(2, 0), 4);
            battle.Advance(0);
            Equal(0f, source.Health);
            Equal(0f, victim.Health);
            Equal(1, victim.LastAttacker.Value);
            Equal(1, battle.LastEffectActivations.Count);
            battle.End();
        }

        private static void HasteAndCriticalApplyToKillerThenExpire()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.SetSkillEnabled(SkillType.Breaker, false);
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            EnemyTarget haste = battle.AddEnemy(new Point2(3, 0), Enemy("haste", 1,
                new DeathEffectDefinition { Type = DeathEffectType.AttackHaste,
                    Duration = 1, IntervalMultiplier = 0.5f }));
            EnemyTarget critical = battle.AddEnemy(new Point2(3, 1), Enemy("critical", 1,
                new DeathEffectDefinition { Type = DeathEffectType.GuaranteedCritical,
                    Duration = 1, CriticalMultiplier = 2 }));
            EnemyTarget normal = battle.AddEnemy(new Point2(0, 0), 20);
            haste.Hit(1, 1);
            critical.Hit(1, 1);
            battle.Advance(0);
            Equal(2f, battle.Buffs.AttackRate(1));
            Equal(2f, battle.Buffs.DamageMultiplier(1));
            battle.SetSkillEnabled(SkillType.Breaker, true);
            battle.Aim = new Point2(0, 0);
            battle.Advance(0);
            Equal(16f, normal.Health);
            battle.Advance(0.5f);
            Equal(12f, normal.Health);
            battle.Advance(0.5f);
            Equal(8f, normal.Health);
            Equal(1f, battle.Buffs.AttackRate(1));
            Equal(1f, battle.Buffs.DamageMultiplier(1));
            battle.Advance(1f);
            Equal(6f, normal.Health);
            battle.End();
        }

        private static void PlayersDoNotShareKillBuffs()
        {
            var buffs = new PlayerBuffs();
            buffs.Grant(2, new DeathEffectDefinition { Type = DeathEffectType.GuaranteedCritical,
                Duration = 3, CriticalMultiplier = 2 });
            Equal(1f, buffs.DamageMultiplier(1));
            Equal(2f, buffs.DamageMultiplier(2));
            buffs.Advance(3);
            Equal(1f, buffs.DamageMultiplier(2));
        }

        private static void CheckThrows(Action action, string message)
        {
            try { action(); }
            catch (ArgumentException) { return; }
            throw new Exception(message);
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
