using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    // SO의 스킬 목록을 검증해 종류별 수치로 색인한다. Unity API는 알지 못한다.
    public sealed class SkillCatalog
    {
        private readonly Dictionary<SkillType, SkillStats> _stats;
        public IReadOnlyList<SkillType> AvailableSkills { get; }

        private SkillCatalog(Dictionary<SkillType, SkillStats> stats)
        {
            _stats = stats;
            var available = new List<SkillType>(stats.Keys);
            available.Sort();
            AvailableSkills = available.AsReadOnly();
        }

        public SkillStats StatsFor(SkillType skill)
        {
            if (!_stats.TryGetValue(skill, out SkillStats stats))
                throw new ArgumentException($"정의되지 않은 Skill: {skill}", nameof(skill));
            return stats.Copy();
        }

        public static SkillLoadResult Load(IReadOnlyList<SkillData> skills)
        {
            var errors = new List<SkillDiagnostic>();
            var stats = new Dictionary<SkillType, SkillStats>();
            if (skills == null || skills.Count == 0)
                errors.Add(new SkillDiagnostic("Skills", "스킬이 하나 이상 필요하다."));
            else for (int i = 0; i < skills.Count; i++)
            {
                SkillData item = skills[i];
                string path = $"Skills[{i}]";
                if (item == null || !Enum.IsDefined(typeof(SkillType), item.Type))
                {
                    errors.Add(new SkillDiagnostic(path + ".Type", "지원하지 않는 스킬 종류다."));
                    continue;
                }
                SkillType type = item.Type;
                if (stats.ContainsKey(type))
                {
                    errors.Add(new SkillDiagnostic(path + ".Type", "스킬 종류가 중복됐다."));
                    continue;
                }
                if (!Valid(type, item.Stats))
                {
                    errors.Add(new SkillDiagnostic(path + ".Stats", "종류에 맞는 유한한 양수 수치가 필요하다."));
                    continue;
                }
                stats.Add(type, item.Stats.Copy());
            }
            return errors.Count == 0
                ? new SkillLoadResult(new SkillCatalog(stats), errors)
                : new SkillLoadResult(null, errors);
        }

        private static bool Positive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
        private static bool Valid(SkillType type, SkillStats value)
        {
            if (value == null || !Positive(value.Damage) || !Positive(value.Interval)) return false;
            return type == SkillType.Breaker
                ? Positive(value.Radius) && value.Width == 0 && value.TelegraphDuration == 0
                : Positive(value.Width) && Positive(value.TelegraphDuration) && value.Radius == 0;
        }
    }

    public sealed class SkillLoadResult
    {
        public SkillCatalog Catalog { get; }
        public IReadOnlyList<SkillDiagnostic> Diagnostics { get; }
        public bool Succeeded => Catalog != null;

        internal SkillLoadResult(SkillCatalog catalog, IReadOnlyList<SkillDiagnostic> errors)
        {
            Catalog = catalog;
            Diagnostics = errors;
        }
    }
}
