using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    [Serializable]
    public sealed class PlayerCombatStats
    {
        public float CritChance = 0;
        public float CritMultiplier = 2;
        public float IntervalMultiplier = 1;

        public PlayerCombatStats Copy() => new PlayerCombatStats
        {
            CritChance = CritChance,
            CritMultiplier = CritMultiplier,
            IntervalMultiplier = IntervalMultiplier
        };

        public bool IsValid() => Finite(CritChance) && CritChance >= 0 && CritChance <= 1 &&
            Finite(CritMultiplier) && CritMultiplier >= 1 &&
            Finite(IntervalMultiplier) && IntervalMultiplier > 0;

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct SkillHit
    {
        public SkillType Skill { get; }
        public int PlayerId { get; }
        public ISkillTarget Target { get; }
        public float Damage { get; }
        public bool IsCritical { get; }

        internal SkillHit(SkillType skill, int playerId, ISkillTarget target, float damage, bool isCritical)
        {
            Skill = skill;
            PlayerId = playerId;
            Target = target;
            Damage = damage;
            IsCritical = isCritical;
        }
    }

    // 범위 선정은 각 Skill이 담당한다. 피해와 치명타는 공격 1회당 여기서 확정한다.
    public sealed class SkillDamage
    {
        private readonly PlayerCombatStats _stats;
        private readonly PlayerBuffs _buffs;
        private readonly ISkillRandom _criticalRandom;
        private readonly List<SkillHit> _hits = new List<SkillHit>();

        public IReadOnlyList<SkillHit> LastHits => _hits;

        public SkillDamage(PlayerCombatStats stats, PlayerBuffs buffs, ISkillRandom criticalRandom)
        {
            if (stats == null || !stats.IsValid()) throw new ArgumentException("유효하지 않은 전투 수치다.", nameof(stats));
            _stats = stats.Copy();
            _buffs = buffs ?? throw new ArgumentNullException(nameof(buffs));
            _criticalRandom = criticalRandom ?? throw new ArgumentNullException(nameof(criticalRandom));
        }

        public void BeginFrame() => _hits.Clear();

        public int Apply(SkillType skill, int playerId, float baseDamage, IReadOnlyList<ISkillTarget> targets)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (targets.Count == 0) return 0;
            bool hasTarget = false;
            foreach (ISkillTarget target in targets)
                if (target.IsAlive) { hasTarget = true; break; }
            if (!hasTarget) return 0;
            float chance = _buffs.IsGuaranteedCritical(playerId) ? 1 : _stats.CritChance;
            bool critical = chance >= 1 || (chance > 0 && _criticalRandom.NextFloat() < chance);
            float multiplier = critical ? Math.Max(_stats.CritMultiplier, _buffs.CriticalMultiplier(playerId)) : 1;
            float damage = baseDamage * multiplier;
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseDamage));

            int count = 0;
            foreach (ISkillTarget target in targets)
            {
                if (!target.IsAlive) continue;
                target.Hit(damage, playerId);
                _hits.Add(new SkillHit(skill, playerId, target, damage, critical));
                count++;
            }
            return count;
        }
    }

    // Battle별 독립 난수 스트림. Laser 방향 스트림과 치명타 스트림은 따로 생성한다.
    public sealed class SeededSkillRandom : ISkillRandom
    {
        private uint _state;
        public SeededSkillRandom(uint seed) => _state = seed == 0 ? 0x9e3779b9u : seed;

        public float NextFloat()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state >> 8) * (1f / 16777216f);
        }
    }
}
