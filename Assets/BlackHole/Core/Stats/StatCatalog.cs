using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 종류별 수치 목록. 기본값은 정의에, 보정은 Loadout/버프에, 계산 규칙은 이곳에 둔다.
    public sealed class StatCatalog
    {
        private readonly Dictionary<string, StatDefinition> _byId = new Dictionary<string, StatDefinition>(StringComparer.Ordinal);
        public IReadOnlyList<StatDefinition> Entries { get; }

        public StatCatalog(params StatDefinition[] entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var copy = (StatDefinition[])entries.Clone();
            foreach (StatDefinition entry in copy)
            {
                if (entry == null || _byId.ContainsKey(entry.Id)) throw new ArgumentException("비어 있거나 중복된 수치 정의다.");
                _byId.Add(entry.Id, entry);
            }
            Entries = Array.AsReadOnly(copy);
        }

        public StatDefinition Get(string id)
        {
            if (id != null && _byId.TryGetValue(id, out StatDefinition definition)) return definition;
            throw new ArgumentException($"공개하지 않은 수치 '{id}'.", nameof(id));
        }

        public void Validate(StatModifier modifier) => Get(modifier.StatId).ValidateModifier(modifier);

        public StatValues Compute(IReadOnlyDictionary<string, float> baseValues, IReadOnlyList<StatModifier> modifiers)
        {
            if (baseValues == null || baseValues.Count != Entries.Count)
                throw new ArgumentException("수치 목록과 기본값 목록이 일치해야 한다.", nameof(baseValues));
            if (modifiers == null) throw new ArgumentNullException(nameof(modifiers));
            foreach (StatModifier modifier in modifiers) Validate(modifier);
            var values = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (StatDefinition definition in Entries)
            {
                if (!baseValues.TryGetValue(definition.Id, out float initial))
                    throw new ArgumentException($"기본 수치 '{definition.Id}'가 없다.");
                definition.Validate(initial);
                // 합산 순서까지 고정한다. 파일 순서와 구매 순서가 부동소수점 결과를 바꾸지 않는다.
                var additions = new List<float>();
                var rates = new List<float>();
                foreach (StatModifier modifier in modifiers)
                {
                    if (modifier.StatId != definition.Id) continue;
                    (modifier.Operation == StatOperation.Add ? additions : rates).Add(modifier.Value);
                }
                additions.Sort();
                rates.Sort();
                double add = 0, rate = 0;
                foreach (float value in additions) add += value;
                foreach (float value in rates) rate += value;
                float result = (float)((initial + add) * (1 + rate));
                values.Add(definition.Id, definition.Validate(result));
            }
            return new StatValues(values);
        }
    }

    public sealed class StatValues
    {
        private readonly IReadOnlyDictionary<string, float> _values;
        internal StatValues(Dictionary<string, float> values)
        {
            _values = new System.Collections.ObjectModel.ReadOnlyDictionary<string, float>(values);
        }
        public float this[string id] => _values[id];
        public IReadOnlyDictionary<string, float> Values => _values;
    }
}
