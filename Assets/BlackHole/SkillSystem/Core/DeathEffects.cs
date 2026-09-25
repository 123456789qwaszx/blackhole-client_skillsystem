using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    public enum DeathEffectType { None, ChainLightning, Explosion, AttackHaste, GuaranteedCritical }

    [Serializable]
    public sealed class DeathEffectDefinition
    {
        public DeathEffectType Type;
        public float Damage;
        public float Radius;
        public int MaxTargets;
        public float Duration;
        public float IntervalMultiplier;
        public float CriticalMultiplier;

        public bool IsValid()
        {
            switch (Type)
            {
                case DeathEffectType.None:
                    return Damage == 0 && Radius == 0 && MaxTargets == 0 && Duration == 0 &&
                        IntervalMultiplier == 0 && CriticalMultiplier == 0;
                case DeathEffectType.ChainLightning:
                    return Positive(Damage) && Positive(Radius) && MaxTargets > 0 && MaxTargets <= 64 &&
                        Duration == 0 && IntervalMultiplier == 0 && CriticalMultiplier == 0;
                case DeathEffectType.Explosion:
                    return Positive(Damage) && Positive(Radius) && MaxTargets == 0 &&
                        Duration == 0 && IntervalMultiplier == 0 && CriticalMultiplier == 0;
                case DeathEffectType.AttackHaste:
                    return Damage == 0 && Radius == 0 && MaxTargets == 0 && Positive(Duration) &&
                        IntervalMultiplier >= 0.05f && IntervalMultiplier < 1 && CriticalMultiplier == 0;
                case DeathEffectType.GuaranteedCritical:
                    return Damage == 0 && Radius == 0 && MaxTargets == 0 && Positive(Duration) &&
                        IntervalMultiplier == 0 && Positive(CriticalMultiplier) && CriticalMultiplier > 1;
                default: return false;
            }
        }

        public DeathEffectDefinition Copy() => (DeathEffectDefinition)MemberwiseClone();

        private static bool Positive(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
    }

    public interface IDeathEffectTarget : ISkillTarget
    {
        DeathEffectDefinition DeathEffect { get; }
        int? LastAttacker { get; }
    }

    public readonly struct EffectHit
    {
        public DeathEffectType Type { get; }
        public Point2 From { get; }
        public Point2 To { get; }
        public EffectHit(DeathEffectType type, Point2 from, Point2 to)
        {
            Type = type;
            From = from;
            To = to;
        }
    }

    public readonly struct EffectActivation
    {
        public DeathEffectType Type { get; }
        public Point2 Position { get; }
        public float Radius { get; }
        public int PlayerId { get; }
        public EffectActivation(DeathEffectType type, Point2 position, float radius, int playerId)
        {
            Type = type;
            Position = position;
            Radius = radius;
            PlayerId = playerId;
        }
    }

    // 적마다 사망을 한 번만 처리한다. 피해 효과는 효과 보유 적을 건드리지 않는다.
    public sealed class DeathEffects
    {
        private readonly HashSet<IDeathEffectTarget> _resolved = new HashSet<IDeathEffectTarget>();
        private readonly List<EffectHit> _hits = new List<EffectHit>();
        private readonly List<EffectActivation> _activations = new List<EffectActivation>();
        public IReadOnlyList<EffectHit> LastHits => _hits;
        public IReadOnlyList<EffectActivation> LastActivations => _activations;

        public void Resolve(IReadOnlyList<IDeathEffectTarget> enemies, PlayerBuffs buffs)
        {
            if (enemies == null) throw new ArgumentNullException(nameof(enemies));
            if (buffs == null) throw new ArgumentNullException(nameof(buffs));
            _hits.Clear();
            _activations.Clear();
            foreach (IDeathEffectTarget enemy in enemies)
            {
                if (enemy.IsAlive || !_resolved.Add(enemy)) continue;
                DeathEffectDefinition effect = enemy.DeathEffect;
                if (effect == null || effect.Type == DeathEffectType.None || !enemy.LastAttacker.HasValue)
                    continue;
                _activations.Add(new EffectActivation(effect.Type, enemy.Position,
                    effect.Radius, enemy.LastAttacker.Value));
                switch (effect.Type)
                {
                    case DeathEffectType.ChainLightning:
                        Chain(enemy, effect, enemies, enemy.LastAttacker.Value);
                        break;
                    case DeathEffectType.Explosion:
                        Explode(enemy, effect, enemies, enemy.LastAttacker.Value);
                        break;
                    case DeathEffectType.AttackHaste:
                    case DeathEffectType.GuaranteedCritical:
                        buffs.Grant(enemy.LastAttacker.Value, effect);
                        break;
                    default: throw new ArgumentOutOfRangeException(nameof(effect.Type));
                }
            }
        }

        private void Explode(IDeathEffectTarget source, DeathEffectDefinition effect,
            IReadOnlyList<IDeathEffectTarget> enemies, int playerId)
        {
            float radiusSquared = effect.Radius * effect.Radius;
            foreach (IDeathEffectTarget target in enemies)
                if (CanHit(target) && source.Position.DistanceSquared(target.Position) <= radiusSquared)
                    Hit(source.Position, target, effect, playerId);
        }

        private void Chain(IDeathEffectTarget source, DeathEffectDefinition effect,
            IReadOnlyList<IDeathEffectTarget> enemies, int playerId)
        {
            var visited = new HashSet<IDeathEffectTarget>();
            Point2 origin = source.Position;
            float radiusSquared = effect.Radius * effect.Radius;
            for (int hop = 0; hop < effect.MaxTargets; hop++)
            {
                IDeathEffectTarget nearest = null;
                float bestDistance = radiusSquared;
                foreach (IDeathEffectTarget target in enemies)
                {
                    if (!CanHit(target) || visited.Contains(target)) continue;
                    float distance = origin.DistanceSquared(target.Position);
                    if (distance > bestDistance) continue;
                    if (nearest != null && distance >= bestDistance) continue;
                    nearest = target;
                    bestDistance = distance;
                }
                if (nearest == null) break;
                visited.Add(nearest);
                Hit(origin, nearest, effect, playerId);
                origin = nearest.Position;
            }
        }

        private void Hit(Point2 origin, IDeathEffectTarget target, DeathEffectDefinition effect, int playerId)
        {
            _hits.Add(new EffectHit(effect.Type, origin, target.Position));
            target.Hit(effect.Damage, playerId);
        }

        private static bool CanHit(IDeathEffectTarget target) => target.IsAlive &&
            (target.DeathEffect == null || target.DeathEffect.Type == DeathEffectType.None);
    }
}
