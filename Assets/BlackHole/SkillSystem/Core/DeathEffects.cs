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

    // 전투마다 생성한다. 같은 종류 재획득은 시간을 갱신하고 강한 값만 남긴다.
    public sealed class PlayerBuffs
    {
        private readonly Dictionary<int, BuffState> _players = new Dictionary<int, BuffState>();

        public void Grant(int playerId, DeathEffectDefinition effect)
        {
            if (effect == null || !effect.IsValid()) throw new ArgumentException("유효하지 않은 효과다.", nameof(effect));
            if (!_players.TryGetValue(playerId, out BuffState state))
                _players.Add(playerId, state = new BuffState());
            switch (effect.Type)
            {
                case DeathEffectType.AttackHaste:
                    state.HasteTime = Math.Max(state.HasteTime, effect.Duration);
                    state.IntervalMultiplier = state.IntervalMultiplier == 0
                        ? effect.IntervalMultiplier : Math.Min(state.IntervalMultiplier, effect.IntervalMultiplier);
                    break;
                case DeathEffectType.GuaranteedCritical:
                    state.CriticalTime = Math.Max(state.CriticalTime, effect.Duration);
                    state.CriticalMultiplier = Math.Max(state.CriticalMultiplier, effect.CriticalMultiplier);
                    break;
                default: throw new ArgumentException("버프 효과가 아니다.", nameof(effect));
            }
        }

        public void Advance(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
            foreach (BuffState state in _players.Values)
            {
                state.HasteTime = Math.Max(0, state.HasteTime - delta);
                state.CriticalTime = Math.Max(0, state.CriticalTime - delta);
                if (state.HasteTime == 0) state.IntervalMultiplier = 0;
                if (state.CriticalTime == 0) state.CriticalMultiplier = 0;
            }
        }

        public float AttackRate(int playerId) => _players.TryGetValue(playerId, out BuffState state) &&
            state.HasteTime > 0 ? 1 / state.IntervalMultiplier : 1;

        public float DamageMultiplier(int playerId) => _players.TryGetValue(playerId, out BuffState state) &&
            state.CriticalTime > 0 ? state.CriticalMultiplier : 1;

        public float HasteRemaining(int playerId) => _players.TryGetValue(playerId, out BuffState state)
            ? state.HasteTime : 0;

        public float CriticalRemaining(int playerId) => _players.TryGetValue(playerId, out BuffState state)
            ? state.CriticalTime : 0;

        private sealed class BuffState
        {
            public float HasteTime;
            public float IntervalMultiplier;
            public float CriticalTime;
            public float CriticalMultiplier;
        }
    }
}
