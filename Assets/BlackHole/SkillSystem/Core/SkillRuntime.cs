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

    public abstract class SkillRuntime
    {
        public SkillType Type { get; }
        protected readonly SkillStats Stats;
        protected readonly int PlayerId;

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

    // 조준점 주변의 살아 있는 적을 정해진 주기로 한 번씩 공격한다.
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
            _untilNext -= delta * attackRate;
            while (_untilNext <= 0.000001f)
            {
                TickCount++;
                _targets.Clear();
                if (aim.HasValue)
                    foreach (ISkillTarget target in targets)
                        if (target.IsAlive && target.Position.DistanceSquared(aim.Value) <= Stats.Radius * Stats.Radius)
                            _targets.Add(target);
                LastHitCount = damage.Apply(Type, PlayerId, Stats.Damage, _targets);
                _untilNext += Stats.Interval;
            }
        }
    }

    public readonly struct LaserShot
    {
        public Point2 Start { get; }
        public Point2 End { get; }
        public float Remaining { get; }
        internal LaserShot(Point2 start, Point2 end, float remaining)
        {
            Start = start; End = end; Remaining = remaining;
        }
        internal LaserShot Elapse(float delta) => new LaserShot(Start, End, Remaining - delta);
    }

    // 예고 시작 때 조준점을 고정하고, 예고가 끝나면 선분 폭 안의 적을 관통한다.
    public sealed class LaserRuntime : SkillRuntime
    {
        private readonly List<LaserShot> _pending = new List<LaserShot>();
        private readonly List<ISkillTarget> _targets = new List<ISkillTarget>();
        private float _untilNext;
        public IReadOnlyList<LaserShot> PendingShots => _pending.AsReadOnly();
        public int FireCount { get; private set; }
        public int LastHitCount { get; private set; }
        public LaserShot? LastFired { get; private set; }

        internal LaserRuntime(SkillStats stats, int playerId) : base(SkillType.PiercingLaser, stats, playerId) { }

        public override float TimeUntilNextEvent(float attackRate)
        {
            float next = Math.Max(0, _untilNext / attackRate);
            foreach (LaserShot shot in _pending) next = Math.Min(next, Math.Max(0, shot.Remaining));
            return next;
        }

        public override void Advance(float delta, Point2? aim, IReadOnlyList<ISkillTarget> targets,
            ISkillRandom random, float arenaRadius, SkillDamage damage, float attackRate)
        {
            CheckDelta(delta);
            CheckAttackRate(attackRate);
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (float.IsNaN(arenaRadius) || float.IsInfinity(arenaRadius) || arenaRadius <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaRadius));

            for (int i = 0; i < _pending.Count; i++) _pending[i] = _pending[i].Elapse(delta);
            _untilNext -= delta * attackRate;
            while (_untilNext <= 0.000001f)
            {
                Telegraph(aim, random, arenaRadius, Stats.TelegraphDuration + _untilNext / attackRate);
                _untilNext += Stats.Interval;
            }
            for (int i = 0; i < _pending.Count;)
            {
                if (_pending[i].Remaining > 0.000001f) { i++; continue; }
                LaserShot shot = _pending[i];
                _pending.RemoveAt(i);
                Fire(shot, targets, damage);
            }
        }

        private void Telegraph(Point2? aim, ISkillRandom random, float radius, float remaining)
        {
            if (!aim.HasValue || aim.Value.DistanceSquared(new Point2(0, 0)) >= radius * radius) return;
            double angle = random.NextFloat() * 2 * Math.PI;
            var start = new Point2(radius * (float)Math.Cos(angle), radius * (float)Math.Sin(angle));
            float dx = aim.Value.X - start.X, dy = aim.Value.Y - start.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            dx /= length; dy /= length;
            float travel = -2 * (start.X * dx + start.Y * dy);
            _pending.Add(new LaserShot(start, new Point2(start.X + travel * dx, start.Y + travel * dy), remaining));
        }

        private void Fire(LaserShot shot, IReadOnlyList<ISkillTarget> targets, SkillDamage damage)
        {
            FireCount++;
            LastFired = shot;
            float widthSquared = Stats.Width * Stats.Width / 4;
            _targets.Clear();
            foreach (ISkillTarget target in targets)
                if (target.IsAlive && DistanceSquaredToSegment(target.Position, shot) <= widthSquared)
                    _targets.Add(target);
            LastHitCount = damage.Apply(Type, PlayerId, Stats.Damage, _targets);
        }

        private static float DistanceSquaredToSegment(Point2 point, LaserShot shot)
        {
            float dx = shot.End.X - shot.Start.X, dy = shot.End.Y - shot.Start.Y;
            float lengthSquared = dx * dx + dy * dy;
            float projection = ((point.X - shot.Start.X) * dx + (point.Y - shot.Start.Y) * dy) / lengthSquared;
            projection = Math.Max(0, Math.Min(1, projection));
            return point.DistanceSquared(new Point2(shot.Start.X + projection * dx, shot.Start.Y + projection * dy));
        }
    }
}
