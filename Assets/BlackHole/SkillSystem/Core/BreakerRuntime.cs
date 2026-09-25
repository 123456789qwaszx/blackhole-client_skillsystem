using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    // 포인터를 중심으로 반경 안의 적을 주기적으로 한 번씩 공격한다.
    public sealed class BreakerRuntime : SkillRuntime
    {
        private readonly List<ISkillTarget> _targets = new List<ISkillTarget>();
        private float _untilNext;

        public int TickCount { get; private set; }
        public int LastHitCount { get; private set; }

        internal BreakerRuntime(SkillStats stats, int playerId) : base(SkillType.Breaker, stats, playerId) { }

        public override float TimeUntilNextEvent(float attackRate) => Math.Max(0, _untilNext / attackRate);

        public override void Advance(float delta, Point2? aim, IReadOnlyList<ISkillTarget> targets,
            ISkillRandom random, float arenaRadius, SkillDamage damage, float attackRate)
        {
            CheckDelta(delta);
            CheckAttackRate(attackRate);
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            Visuals.Clear();
            _untilNext -= delta * attackRate;
            while (_untilNext <= 0.000001f)
            {
                TickCount++;
                _targets.Clear();
                if (aim.HasValue)
                {
                    Visuals.Add(new SkillVisual(PlayerId, aim.Value, Stats.Radius));
                    foreach (ISkillTarget target in targets)
                        if (target.IsAlive && target.Position.DistanceSquared(aim.Value) <= Stats.Radius * Stats.Radius)
                            _targets.Add(target);
                }
                LastHitCount = damage.Apply(Type, PlayerId, Stats.Damage, _targets);
                _untilNext += Stats.Interval;
            }
        }
    }
}
