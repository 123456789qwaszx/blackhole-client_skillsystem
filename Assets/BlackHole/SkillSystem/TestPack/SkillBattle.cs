using System;
using System.Collections.Generic;

namespace BlackHole.Skills.TestPack
{
    // 이동·보상·성장 없이 스킬 실행만 확인하는 작은 전투 팩.
    public sealed class SkillBattle
    {
        private readonly SkillProgress _progress;
        private readonly List<EnemyTarget> _enemies = new List<EnemyTarget>();
        private readonly List<SkillRuntime> _skills = new List<SkillRuntime>();
        private readonly ISkillRandom _random;
        private readonly float _arenaRadius;
        private readonly int _playerId;
        private bool _ended;

        public IReadOnlyList<EnemyTarget> Enemies => _enemies.AsReadOnly();
        public IReadOnlyList<SkillRuntime> Skills => _skills.AsReadOnly();
        public Point2? Aim { get; set; }

        public SkillBattle(SkillProgress progress, int playerId, float arenaRadius, ISkillRandom random)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (float.IsNaN(arenaRadius) || float.IsInfinity(arenaRadius) || arenaRadius <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaRadius));
            _progress = progress;
            _playerId = playerId;
            _random = random;
            _arenaRadius = arenaRadius;

            IReadOnlyDictionary<SkillType, UpgradeStat> stats = progress.Snapshot();
            foreach (SkillType type in (SkillType[])Enum.GetValues(typeof(SkillType)))
                if (stats.TryGetValue(type, out UpgradeStat value))
                    _skills.Add(SkillRuntime.Create(type, value, playerId));
            _progress.BeginBattle();
        }

        public EnemyTarget AddEnemy(Point2 position, float health)
        {
            if (_ended) throw new InvalidOperationException("종료된 전투다.");
            var enemy = new EnemyTarget(position, health);
            _enemies.Add(enemy);
            return enemy;
        }

        public void Advance(float delta)
        {
            if (_ended) throw new InvalidOperationException("종료된 전투다.");
            IReadOnlyList<ISkillTarget> targets = _enemies;
            foreach (SkillRuntime skill in _skills)
                skill.Advance(delta, Aim, targets, _random, _arenaRadius);
        }

        public void End()
        {
            if (_ended) return;
            _ended = true;
            _progress.EndBattle();
        }
    }

    // 테스트·샘플에서 재현 가능한 발사 방향.
    public sealed class FixedRandom : ISkillRandom
    {
        private readonly float _value;
        public FixedRandom(float value)
        {
            if (value < 0 || value >= 1 || float.IsNaN(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            _value = value;
        }
        public float NextFloat() => _value;
    }
}
