using System;

namespace BlackHole.Skills.TestPack
{
    // Skill이 실제로 적에게 피해를 주는지만 확인하는 최소 표적.
    public sealed class EnemyTarget : IDeathEffectTarget
    {
        public string Id { get; }
        public Point2 Position { get; }
        public DeathEffectDefinition DeathEffect { get; }
        public float MaxHealth { get; }
        public float Health { get; private set; }
        public bool IsAlive => Health > 0;
        public int? LastAttacker { get; private set; }

        public EnemyTarget(Point2 position, float health)
            : this(position, new EnemyDefinition
                { Id = "target", Health = health, DeathEffect = new DeathEffectDefinition() }) { }

        public EnemyTarget(Point2 position, EnemyDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) ||
                float.IsNaN(definition.Health) || float.IsInfinity(definition.Health) ||
                definition.Health <= 0 || definition.DeathEffect == null || !definition.DeathEffect.IsValid())
                throw new ArgumentException("유효하지 않은 적 정의다.", nameof(definition));
            Id = definition.Id;
            DeathEffect = definition.DeathEffect.Copy();
            Position = position;
            MaxHealth = definition.Health;
            Health = definition.Health;
        }

        public void Hit(float damage, int playerId)
        {
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0)
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (!IsAlive) return;
            Health = Math.Max(0, Health - damage);
            LastAttacker = playerId;
        }
    }
}
