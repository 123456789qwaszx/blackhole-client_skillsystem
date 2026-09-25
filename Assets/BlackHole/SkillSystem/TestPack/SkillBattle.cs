using System;
using System.Collections.Generic;

namespace BlackHole.Skills.TestPack
{
    // 이동·보상·성장 없이 스킬 공격과 천체 사망 효과를 확인하는 전투 팩.
    public sealed class SkillBattle
    {
        private readonly List<EnemyTarget> _enemies = new List<EnemyTarget>();
        private readonly List<SkillRuntime> _skills = new List<SkillRuntime>();
        private readonly List<float> _baseIntervals = new List<float>();
        private readonly List<float> _stepRates = new List<float>();
        private readonly List<bool> _enabled = new List<bool>();
        private readonly DeathEffects _deathEffects = new DeathEffects();
        private readonly PlayerBuffs _buffs = new PlayerBuffs();
        private readonly SkillDamage _damage;
        private readonly PlayerCombatStats _combatStats;
        private readonly List<EffectHit> _recentEffectHits = new List<EffectHit>();
        private readonly List<EffectActivation> _recentEffectActivations = new List<EffectActivation>();
        private readonly List<SkillVisual> _recentSkillVisuals = new List<SkillVisual>();
        private readonly SkillCatalog _catalog;
        private readonly ISkillRandom _random;
        private readonly float _arenaRadius;
        private readonly int _playerId;
        private bool _ended;
        private float _unprocessedTime;

        public IReadOnlyList<EnemyTarget> Enemies => _enemies.AsReadOnly();
        public IReadOnlyList<SkillRuntime> Skills => _skills.AsReadOnly();
        public IReadOnlyList<EffectHit> LastEffectHits => _recentEffectHits;
        public IReadOnlyList<EffectActivation> LastEffectActivations => _recentEffectActivations;
        public IReadOnlyList<SkillHit> LastSkillHits => _damage.LastHits;
        public IReadOnlyList<SkillVisual> LastSkillVisuals => _recentSkillVisuals;
        public PlayerBuffs Buffs => _buffs;
        public Point2? Aim { get; set; }

        public SkillBattle(SkillCatalog catalog, int playerId, float arenaRadius, uint battleSeed,
            PlayerCombatStats combatStats = null)
            : this(catalog, playerId, arenaRadius,
                new SeededSkillRandom(battleSeed ^ unchecked((uint)playerId * 747796405u) ^ 0x4d5b0201u),
                combatStats,
                new SeededSkillRandom(battleSeed ^ unchecked((uint)playerId * 747796405u) ^ 0xc7a22413u)) { }

        public SkillBattle(SkillCatalog catalog, int playerId, float arenaRadius, ISkillRandom random,
            PlayerCombatStats combatStats = null, ISkillRandom criticalRandom = null)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (float.IsNaN(arenaRadius) || float.IsInfinity(arenaRadius) || arenaRadius <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaRadius));
            _catalog = catalog;
            _playerId = playerId;
            _random = random;
            _arenaRadius = arenaRadius;
            _combatStats = (combatStats ?? new PlayerCombatStats()).Copy();
            if (!_combatStats.IsValid()) throw new ArgumentException("유효하지 않은 플레이어 전투 수치다.", nameof(combatStats));
            _damage = new SkillDamage(_combatStats, _buffs,
                criticalRandom ?? new SeededSkillRandom(unchecked((uint)playerId * 747796405u + 2891336453u)));

            foreach (SkillType type in catalog.AvailableSkills)
            {
                SkillStats stats = catalog.StatsFor(type);
                _skills.Add(SkillRuntime.Create(type, stats, playerId));
                _baseIntervals.Add(stats.Interval);
                _stepRates.Add(1);
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
            return AddEnemy(position, new EnemyDefinition
                { Id = "target", Health = health, DeathEffect = new DeathEffectDefinition() });
        }

        public EnemyTarget AddEnemy(Point2 position, EnemyDefinition definition)
        {
            if (_ended) throw new InvalidOperationException("종료된 전투다.");
            var enemy = new EnemyTarget(position, definition);
            _enemies.Add(enemy);
            return enemy;
        }

        public void Advance(float delta)
        {
            if (_ended) throw new InvalidOperationException("종료된 전투다.");
            CheckDelta(delta);
            BeginFrame();
            AdvanceStep(0);
            float remaining = delta;
            while (remaining > 0.000001f)
            {
                float step = NextStep(remaining);
                AdvanceStep(step);
                remaining -= step;
            }
        }

        // Unity 프레임당 최대 0.2초 처리. 미처리 시간은 다음 프레임에 이어서 실행한다.
        public void AdvanceFrame(float delta)
        {
            if (_ended) throw new InvalidOperationException("종료된 전투다.");
            CheckDelta(delta);
            _unprocessedTime += delta;
            BeginFrame();
            AdvanceStep(0);
            for (int i = 0; i < 4 && _unprocessedTime > 0.000001f; i++)
            {
                float step = NextStep(_unprocessedTime);
                AdvanceStep(step);
                _unprocessedTime -= step;
            }
        }

        private void BeginFrame()
        {
            _damage.BeginFrame();
            _recentEffectHits.Clear();
            _recentEffectActivations.Clear();
            _recentSkillVisuals.Clear();
        }

        private float NextStep(float remaining)
        {
            float step = Math.Min(0.05f, remaining);
            float expiry = _buffs.TimeUntilExpiry(_playerId);
            if (expiry > 0 && expiry < step) step = expiry;
            for (int i = 0; i < _skills.Count; i++)
            {
                if (!_enabled[i]) continue;
                float untilEvent = _skills[i].TimeUntilNextEvent(AttackRate(i));
                if (untilEvent > 0.000001f && untilEvent < step) step = untilEvent;
            }
            return step;
        }

        private void AdvanceStep(float delta)
        {
            IReadOnlyList<ISkillTarget> targets = _enemies;
            // 기간을 먼저 만료시키되 이번 Step의 공격 진행 속도는 경계 이전 값을 쓴다.
            for (int i = 0; i < _skills.Count; i++)
                if (_enabled[i]) _stepRates[i] = AttackRate(i);
            _buffs.Advance(delta);
            for (int i = 0; i < _skills.Count; i++)
            {
                if (!_enabled[i]) continue;
                _skills[i].Advance(delta, Aim, targets, _random, _arenaRadius, _damage, _stepRates[i]);
                foreach (SkillVisual visual in _skills[i].LastVisuals) _recentSkillVisuals.Add(visual);
            }
            IReadOnlyList<IDeathEffectTarget> effectTargets = _enemies;
            _deathEffects.Resolve(effectTargets, _buffs);
            foreach (EffectHit hit in _deathEffects.LastHits) _recentEffectHits.Add(hit);
            foreach (EffectActivation activation in _deathEffects.LastActivations)
                _recentEffectActivations.Add(activation);
        }

        private float AttackRate(int index)
        {
            float baseInterval = _baseIntervals[index];
            float multiplier = _combatStats.IntervalMultiplier * _buffs.IntervalMultiplier(_playerId);
            return baseInterval / Math.Max(0.1f, baseInterval * multiplier);
        }

        private static void CheckDelta(float delta)
        {
            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0)
                throw new ArgumentOutOfRangeException(nameof(delta));
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
