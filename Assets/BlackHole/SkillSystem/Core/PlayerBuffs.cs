using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
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
