using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
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

    // 예고 시작 시 방향을 고정하고, 발사 시 선분 폭 안의 적을 관통한다.
    public sealed class LaserRuntime : SkillRuntime
    {
        private readonly List<LaserShot> _pending = new List<LaserShot>();
        private readonly List<ISkillTarget> _targets = new List<ISkillTarget>();
        private float _untilNext;

        public IReadOnlyList<LaserShot> PendingShots => _pending.AsReadOnly();
        public float TelegraphDuration => Stats.TelegraphDuration;
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

            Visuals.Clear();
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
            Visuals.Add(new SkillVisual(PlayerId, shot.Start, shot.End, Stats.Width));
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
