using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    public enum StatOperation { Add, Rate }
    public enum StatValueKind { Number, Integer }

    // 도구와 런타임이 함께 읽는 수치의 계약. 범위는 기본값과 합성 결과에 적용한다.
    public sealed class StatDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Unit { get; }
        public StatValueKind ValueKind { get; }
        public float Minimum { get; }
        public float Maximum { get; }
        public IReadOnlyList<StatOperation> Operations { get; }

        public StatDefinition(string id, string displayName, string unit,
            float minimum, float maximum, StatValueKind valueKind = StatValueKind.Number,
            params StatOperation[] operations)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("수치 ID가 비어 있다.", nameof(id));
            if (float.IsNaN(minimum) || float.IsInfinity(minimum) ||
                float.IsNaN(maximum) || float.IsInfinity(maximum) || minimum > maximum)
                throw new ArgumentException("유한한 수치 범위가 필요하다.");
            if (!Enum.IsDefined(typeof(StatValueKind), valueKind)) throw new ArgumentOutOfRangeException(nameof(valueKind));
            Id = id;
            DisplayName = displayName;
            Unit = unit;
            Minimum = minimum;
            Maximum = maximum;
            ValueKind = valueKind;
            StatOperation[] allowed = operations == null || operations.Length == 0
                ? new[] { StatOperation.Add, StatOperation.Rate } : (StatOperation[])operations.Clone();
            foreach (StatOperation operation in allowed)
                if (!Enum.IsDefined(typeof(StatOperation), operation)) throw new ArgumentOutOfRangeException(nameof(operations));
            Operations = Array.AsReadOnly(allowed);
        }

        public float Validate(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < Minimum || value > Maximum ||
                (ValueKind == StatValueKind.Integer && value != Math.Floor(value)))
                throw new ArgumentOutOfRangeException(nameof(value), $"수치 '{Id}'는 {Minimum}~{Maximum} 범위의 {ValueKind}이어야 한다.");
            return value;
        }

        public void ValidateModifier(StatModifier modifier)
        {
            if (modifier.StatId != Id) throw new ArgumentException($"수치 '{Id}'의 보정이 아니다.");
            foreach (StatOperation operation in Operations)
                if (operation == modifier.Operation) return;
            throw new ArgumentException($"수치 '{Id}'는 {modifier.Operation} 연산을 받지 않는다.");
        }
    }

    // 대상은 소유 측에서 선택한다. 이 값에는 노드·구매·Player에 대한 지식이 없다.
    public readonly struct StatModifier
    {
        public string StatId { get; }
        public StatOperation Operation { get; }
        public float Value { get; }

        public StatModifier(string statId, StatOperation operation, float value)
        {
            if (string.IsNullOrWhiteSpace(statId)) throw new ArgumentException("수치 ID가 비어 있다.", nameof(statId));
            if (!Enum.IsDefined(typeof(StatOperation), operation)) throw new ArgumentOutOfRangeException(nameof(operation));
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value));
            StatId = statId;
            Operation = operation;
            Value = value;
        }
    }

    // 문자열 경로를 반사로 실행하지 않는다. Skill ID와 그 Skill이 공개한 수치 ID다.
    public readonly struct StatAddress
    {
        public string SkillId { get; }
        public string StatId { get; }

        public StatAddress(string skillId, string statId)
        {
            if (string.IsNullOrWhiteSpace(skillId) || string.IsNullOrWhiteSpace(statId))
                throw new ArgumentException("Skill ID와 수치 ID가 필요하다.");
            SkillId = skillId;
            StatId = statId;
        }

        public override string ToString() => $"{SkillId}/{StatId}";
    }
}
