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
            yield return ("Crit.OneRollForAllTargets", OneRollForAllTargets);
            yield return ("Crit.LaserChecksBuffOnFire", LaserChecksBuffOnFire);
            yield return ("Haste.PreservesCooldownProgress", HastePreservesCooldownProgress);
            yield return ("Battle.FrameBudgetCarriesUnprocessedTime", FrameBudgetCarriesUnprocessedTime);
            yield return ("Battle.AttackIntervalHasMinimum", AttackIntervalHasMinimum);
            yield return ("Buff.CriticalExpiresAtAttackBoundary", CriticalExpiresAtAttackBoundary);
            yield return ("Crit.DirectionStreamIsIndependent", DirectionStreamIsIndependent);
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
            Equal(0.5f, battle.Buffs.IntervalMultiplier(1));
            Check(battle.Buffs.IsGuaranteedCritical(1), "치명타 버프가 부여되어야 한다.");
            battle.SetSkillEnabled(SkillType.Breaker, true);
            battle.Aim = new Point2(0, 0);
            battle.Advance(0);
            Equal(16f, normal.Health);
            battle.Advance(0.5f);
            Equal(12f, normal.Health);
            battle.Advance(0.5f);
            Equal(10f, normal.Health);
            Equal(1f, battle.Buffs.IntervalMultiplier(1));
            Check(!battle.Buffs.IsGuaranteedCritical(1), "치명타 버프가 만료되어야 한다.");
            battle.Advance(1f);
            Equal(8f, normal.Health);
            battle.End();
        }

        private static void PlayersDoNotShareKillBuffs()
        {
            var buffs = new PlayerBuffs();
            buffs.Grant(2, new DeathEffectDefinition { Type = DeathEffectType.GuaranteedCritical,
                Duration = 3, CriticalMultiplier = 2 });
            Check(!buffs.IsGuaranteedCritical(1), "다른 플레이어는 버프를 받지 않는다.");
            Check(buffs.IsGuaranteedCritical(2), "처치자만 버프를 받는다.");
            buffs.Advance(3);
            Check(!buffs.IsGuaranteedCritical(2), "버프가 만료된다.");
        }

        private static void OneRollForAllTargets()
        {
            var roll = new CountingRandom(0.25f);
            var combat = new PlayerCombatStats { CritChance = 0.5f, CritMultiplier = 2 };
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0), combat, roll);
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            battle.Aim = new Point2(0, 0);
            EnemyTarget a = battle.AddEnemy(new Point2(0, 0), 10);
            EnemyTarget b = battle.AddEnemy(new Point2(0.5f, 0), 10);
            battle.Advance(0);
            Equal(6f, a.Health);
            Equal(6f, b.Health);
            Equal(1, roll.Count);
            Equal(2, battle.LastSkillHits.Count);
            Check(battle.LastSkillHits[0].IsCritical && battle.LastSkillHits[1].IsCritical,
                "같은 공격의 적중은 치명타 결과를 공유한다.");
            battle.Aim = new Point2(8, 8);
            battle.Advance(1);
            Equal(1, roll.Count);
            battle.End();
        }

        private static void LaserChecksBuffOnFire()
        {
            var direction = new CountingRandom(0);
            var criticalRoll = new CountingRandom(0.9f);
            var battle = new SkillBattle(Catalog(), 1, 10, direction,
                new PlayerCombatStats(), criticalRoll);
            battle.SetSkillEnabled(SkillType.Breaker, false);
            battle.Aim = new Point2(0, 0);
            EnemyTarget target = battle.AddEnemy(new Point2(2, 0), 20);
            EnemyTarget source = battle.AddEnemy(new Point2(0, 3), Enemy("critical", 1,
                new DeathEffectDefinition { Type = DeathEffectType.GuaranteedCritical,
                    Duration = 1, CriticalMultiplier = 2 }));
            battle.Advance(0);
            Equal(1, direction.Count);
            source.Hit(1, 1);
            battle.Advance(0);
            battle.Advance(0.4f);
            Equal(14f, target.Health);
            Equal(0, criticalRoll.Count);
            Check(battle.LastSkillHits[0].IsCritical, "발사 시 버프가 적용된다.");
            battle.End();

            var expired = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            expired.SetSkillEnabled(SkillType.Breaker, false);
            expired.Aim = new Point2(0, 0);
            EnemyTarget plain = expired.AddEnemy(new Point2(2, 0), 20);
            EnemyTarget shortBuff = expired.AddEnemy(new Point2(0, 3), Enemy("critical", 1,
                new DeathEffectDefinition { Type = DeathEffectType.GuaranteedCritical,
                    Duration = 0.2f, CriticalMultiplier = 2 }));
            expired.Advance(0);
            shortBuff.Hit(1, 1);
            expired.Advance(0);
            expired.Advance(0.4f);
            Equal(17f, plain.Health);
            Check(!expired.LastSkillHits[0].IsCritical, "예고 중 만료한 버프는 발사에 적용되지 않는다.");
            expired.End();
        }

        private static void HastePreservesCooldownProgress()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            battle.Aim = new Point2(0, 0);
            EnemyTarget target = battle.AddEnemy(new Point2(0, 0), 20);
            battle.Advance(0);
            battle.Advance(0.5f);
            Equal(18f, target.Health);
            battle.Buffs.Grant(1, new DeathEffectDefinition { Type = DeathEffectType.AttackHaste,
                Duration = 0.2f, IntervalMultiplier = 0.5f });
            battle.Advance(0.2f);
            Equal(18f, target.Health);
            battle.Advance(0.05f);
            Equal(18f, target.Health);
            battle.Advance(0.05f);
            Equal(16f, target.Health);
            battle.End();
        }

        private static void FrameBudgetCarriesUnprocessedTime()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.Buffs.Grant(1, new DeathEffectDefinition { Type = DeathEffectType.AttackHaste,
                Duration = 1, IntervalMultiplier = 0.5f });
            battle.AdvanceFrame(1);
            Check(Math.Abs(battle.Buffs.HasteRemaining(1) - 0.8f) < 0.0001f,
                "첫 렌더 프레임은 최대 네 Step만 처리한다.");
            battle.AdvanceFrame(0);
            Check(Math.Abs(battle.Buffs.HasteRemaining(1) - 0.6f) < 0.0001f,
                "남은 시간은 다음 프레임에 이어서 처리한다.");
            battle.End();
        }

        private static void AttackIntervalHasMinimum()
        {
            var combat = new PlayerCombatStats { IntervalMultiplier = 0.01f };
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0), combat);
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            battle.Aim = new Point2(0, 0);
            EnemyTarget target = battle.AddEnemy(new Point2(0, 0), 100);
            battle.Advance(0.2f);
            Equal(94f, target.Health);
            Equal(3, ((BreakerRuntime)battle.Skills[0]).TickCount);
            battle.End();
            Check(!new PlayerCombatStats { CritChance = 1.1f }.IsValid(), "확률은 100%를 넘을 수 없다.");
        }

        private static void CriticalExpiresAtAttackBoundary()
        {
            var battle = new SkillBattle(Catalog(), 1, 10, new FixedRandom(0));
            battle.SetSkillEnabled(SkillType.PiercingLaser, false);
            battle.Aim = new Point2(0, 0);
            EnemyTarget target = battle.AddEnemy(new Point2(0, 0), 20);
            battle.Buffs.Grant(1, new DeathEffectDefinition { Type = DeathEffectType.GuaranteedCritical,
                Duration = 0.1f, CriticalMultiplier = 2 });
            battle.Advance(0);
            Equal(16f, target.Health);
            battle.Buffs.Grant(1, new DeathEffectDefinition { Type = DeathEffectType.AttackHaste,
                Duration = 0.1f, IntervalMultiplier = 0.1f });
            battle.Advance(0.1f);
            Equal(14f, target.Health);
            Check(!battle.LastSkillHits[0].IsCritical, "만료와 같은 시각의 공격은 치명타가 아니다.");
            battle.End();
        }

        private static void DirectionStreamIsIndependent()
        {
            SkillCatalog catalog = Catalog();
            var plain = new SkillBattle(catalog, 1, 10, 123u);
            var critical = new SkillBattle(catalog, 1, 10, 123u,
                new PlayerCombatStats { CritChance = 0.5f });
            plain.SetSkillEnabled(SkillType.Breaker, false);
            critical.SetSkillEnabled(SkillType.Breaker, false);
            plain.Aim = critical.Aim = new Point2(0, 0);
            plain.AddEnemy(new Point2(0, 0), 100);
            critical.AddEnemy(new Point2(0, 0), 100);
            plain.Advance(3);
            critical.Advance(3);
            LaserShot a = ((LaserRuntime)plain.Skills[0]).PendingShots[0];
            LaserShot b = ((LaserRuntime)critical.Skills[0]).PendingShots[0];
            Equal(a.Start.X, b.Start.X);
            Equal(a.Start.Y, b.Start.Y);
            plain.End(); critical.End();
        }

        private sealed class CountingRandom : ISkillRandom
        {
            private readonly float _value;
            public int Count { get; private set; }
            public CountingRandom(float value) => _value = value;
            public float NextFloat() { Count++; return _value; }
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
