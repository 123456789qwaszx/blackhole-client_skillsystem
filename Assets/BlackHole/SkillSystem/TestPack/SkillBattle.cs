using System;
using System.Collections.Generic;

namespace BlackHole.Skills.TestPack
{
    // 이동·보상·성장 없이 스킬 실행만 확인하는 작은 전투 팩.
    public sealed class SkillBattle
    {
        private readonly List<EnemyTarget> _enemies = new List<EnemyTarget>();
        private readonly List<SkillRuntime> _skills = new List<SkillRuntime>();
        private readonly List<bool> _enabled = new List<bool>();
        private readonly SkillCatalog _catalog;
        private readonly ISkillRandom _random;
        private readonly float _arenaRadius;
        private readonly int _playerId;
        private bool _ended;

        public IReadOnlyList<EnemyTarget> Enemies => _enemies.AsReadOnly();
        public IReadOnlyList<SkillRuntime> Skills => _skills.AsReadOnly();
        public Point2? Aim { get; set; }

        public SkillBattle(SkillCatalog catalog, int playerId, float arenaRadius, ISkillRandom random)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (float.IsNaN(arenaRadius) || float.IsInfinity(arenaRadius) || arenaRadius <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaRadius));
            _catalog = catalog;
            _playerId = playerId;
            _random = random;
            _arenaRadius = arenaRadius;

            foreach (SkillType type in catalog.AvailableSkills)
            {
                _skills.Add(SkillRuntime.Create(type, catalog.StatsFor(type), playerId));
                _enabled.Add(true);
            }
        }

        public bool IsSkillEnabled(SkillType type)
        {
            for (int i = 0; i < _skills.Count; i++)
                if (_skills[i].Type == type) return _enabled[i];
            return false;
        }

        public bool SetSkillEnabled(SkillType type, bool enabled)
        {
            if (_ended) throw new InvalidOperationException("종료된 전투다.");
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i].Type != type) continue;
                if (_enabled[i] == enabled) return true;
                _enabled[i] = enabled;
                // 중지할 때 진행 중인 예고와 타이머도 버린다. 재활성화는 새 실행으로 시작한다.
                if (!enabled) _skills[i] = SkillRuntime.Create(type, _catalog.StatsFor(type), _playerId);
                return true;
            }
            return false;
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
            for (int i = 0; i < _skills.Count; i++)
                if (_enabled[i]) _skills[i].Advance(delta, Aim, targets, _random, _arenaRadius);
        }

        public void End()
        {
            if (_ended) return;
            _ended = true;
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
