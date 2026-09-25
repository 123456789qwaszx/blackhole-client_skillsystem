using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    public readonly struct Point2
    {
        public float X { get; }
        public float Y { get; }

        public Point2(float x, float y) { X = x; Y = y; }
        public float DistanceSquared(Point2 other)
        {
            float dx = X - other.X, dy = Y - other.Y;
            return dx * dx + dy * dy;
        }
    }

    // 게임이 적을 연결할 때도 이 작은 경계만 구현한다. 테스트 팩은 아래 인터페이스의 한 구현이다.
    public interface ISkillTarget
    {
        Point2 Position { get; }
        bool IsAlive { get; }
        void Hit(float damage, int playerId);
    }

    public interface ISkillRandom { float NextFloat(); }

    public enum SkillVisualKind { BreakerPulse, LaserFire }

    // 공격 형상만 기록한다. 화면은 피해를 다시 판정하지 않는다.
    public readonly struct SkillVisual
    {
        public SkillVisualKind Kind { get; }
        public int PlayerId { get; }
        public Point2 Center { get; }
        public float Radius { get; }
        public Point2 Start { get; }
        public Point2 End { get; }
        public float Width { get; }

        internal SkillVisual(int playerId, Point2 center, float radius)
        {
            Kind = SkillVisualKind.BreakerPulse;
            PlayerId = playerId;
            Center = center; Radius = radius;
            Start = default; End = default; Width = 0;
        }

        internal SkillVisual(int playerId, Point2 start, Point2 end, float width)
        {
            Kind = SkillVisualKind.LaserFire;
            PlayerId = playerId;
            Start = start; End = end; Width = width;
            Center = default; Radius = 0;
        }
    }

    public abstract class SkillRuntime
    {
        public SkillType Type { get; }
        protected readonly SkillStats Stats;
        protected readonly int PlayerId;
        protected readonly List<SkillVisual> Visuals = new List<SkillVisual>();
        public IReadOnlyList<SkillVisual> LastVisuals => Visuals;

        protected SkillRuntime(SkillType type, SkillStats stats, int playerId)
        {
            Type = type;
            Stats = stats.Copy();
            PlayerId = playerId;
        }

        public abstract void Advance(float delta, Point2? aim, IReadOnlyList<ISkillTarget> targets,
            ISkillRandom random, float arenaRadius, SkillDamage damage, float attackRate);

        // 다음 공격 또는 예고 완료까지 남은 전투 시간. Battle이 같은 시각의 이벤트를 묶는다.
        public abstract float TimeUntilNextEvent(float attackRate);

        public static SkillRuntime Create(SkillType type, SkillStats stats, int playerId)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            switch (type)
            {
                case SkillType.Breaker: return new BreakerRuntime(stats, playerId);
                case SkillType.PiercingLaser: return new LaserRuntime(stats, playerId);
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        protected static void CheckDelta(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
        }

        protected static void CheckAttackRate(float attackRate)
        {
            if (float.IsNaN(attackRate) || float.IsInfinity(attackRate) || attackRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackRate));
        }
    }

}
