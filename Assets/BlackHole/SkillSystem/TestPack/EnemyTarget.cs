using System;

namespace BlackHole.Skills.TestPack
{
    // Skill이 실제로 적에게 피해를 주는지만 확인하는 최소 표적.
    public sealed class EnemyTarget : ISkillTarget
    {
        public Point2 Position { get; }
        public float MaxHealth { get; }
        public float Health { get; private set; }
        public bool IsAlive => Health > 0;
        public int? LastAttacker { get; private set; }

        public EnemyTarget(Point2 position, float health)
        {
            if (float.IsNaN(health) || float.IsInfinity(health) || health <= 0)
                throw new ArgumentOutOfRangeException(nameof(health));
            Position = position;
            MaxHealth = health;
            Health = health;
        }

        public void Hit(float damage, int playerId)
        {
            if (!IsAlive) return;
            Health = Math.Max(0, Health - damage);
            LastAttacker = playerId;
        }
    }
}
