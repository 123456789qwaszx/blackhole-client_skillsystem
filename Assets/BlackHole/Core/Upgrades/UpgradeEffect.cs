using System;

namespace BlackHole.Core
{
    public enum UpgradeEffectKind
    {
        SkillStat,
        SkillUnlock,
        EnemyHealthMultiply,
        GoldMultiply,
        HqExpMultiply,
        GrowthSupplyAdd
    }

    // 노드는 주소와 보정을 전달한다. Skill 종류나 실행 수치 구조체를 알지 않는다.
    public sealed class UpgradeEffect
    {
        public UpgradeEffectKind Kind { get; }
        public float Value { get; }
        public PassiveSkillDefinition Skill { get; }
        public EnemyDefinition Enemy { get; }
        public StatModifier? Modifier { get; }
        public StatAddress? Address => Modifier.HasValue
            ? new StatAddress(Skill.Id, Modifier.Value.StatId) : (StatAddress?)null;

        public UpgradeEffect(UpgradeEffectKind kind, float value,
            PassiveSkillDefinition skill = null, EnemyDefinition enemy = null,
            StatModifier? modifier = null)
        {
            if (!Enum.IsDefined(typeof(UpgradeEffectKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (TargetsSkill(kind) != (skill != null)) throw new ArgumentException("Skill 대상이 종류와 맞지 않는다.");
            if (TargetsSkill(kind) && enemy != null) throw new ArgumentException("Skill 효과는 적 대상을 받지 않는다.");
            if (RequiresEnemy(kind) && enemy == null) throw new ArgumentException("대상 적이 필요하다.");
            if (kind == UpgradeEffectKind.SkillStat)
            {
                if (!modifier.HasValue) throw new ArgumentException("수치 주소와 보정이 필요하다.");
                if (value != modifier.Value.Value) throw new ArgumentException("보정 값이 일치하지 않는다.");
                skill.Stats.Validate(modifier.Value);
            }
            else
            {
                if (modifier.HasValue) throw new ArgumentException("수치 보정을 받지 않는 종류다.");
                if (HasValue(kind)) DefinitionGuard.Positive(value, nameof(value));
                else if (value != 0) throw new ArgumentException("해금은 값을 받지 않는다.");
            }
            if (kind == UpgradeEffectKind.GrowthSupplyAdd && (value != Math.Floor(value) || value >= int.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(value), "공급 보정은 int 범위의 양의 정수다.");
            Kind = kind;
            Value = value;
            Skill = skill;
            Enemy = enemy;
            Modifier = modifier;
        }

        public static bool TargetsSkill(UpgradeEffectKind kind) =>
            kind == UpgradeEffectKind.SkillStat || kind == UpgradeEffectKind.SkillUnlock;
        public static bool RequiresEnemy(UpgradeEffectKind kind) => kind == UpgradeEffectKind.GrowthSupplyAdd;
        public static bool HasValue(UpgradeEffectKind kind) => kind != UpgradeEffectKind.SkillUnlock;
    }
}
