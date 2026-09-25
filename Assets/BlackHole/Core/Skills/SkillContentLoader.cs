using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // JSON 파서는 바깥에 둔다. 이 로더는 gameplay.json의 스킬 두 필드를 DTO로 받아
    // 종류별 정의와 ID 참조를 검사하고, 오류가 있으면 실행 가능한 정의를 돌려주지 않는다.
    public static class SkillContentLoader
    {
        public static SkillLoadResult Load(IReadOnlyList<SkillData> items, IReadOnlyList<string> startingSkills)
        {
            var diagnostics = new List<ContentDiagnostic>();
            var definitions = new List<PassiveSkillDefinition>();

            if (items == null)
                diagnostics.Add(new ContentDiagnostic("Skills", "Skill 목록이 없다."));
            else
                for (int i = 0; i < items.Count; i++)
                {
                    SkillData item = items[i];
                    string at = string.IsNullOrWhiteSpace(item?.Id) ? $"Skills[{i}]" : $"Skills[{item.Id}]";
                    if (item == null)
                    {
                        diagnostics.Add(new ContentDiagnostic(at, "Skill 데이터가 null이다."));
                        continue;
                    }

                    PassiveSkillDefinition skill = LoadSkill(item, at, diagnostics);
                    if (skill != null) definitions.Add(skill);
                }

            if (startingSkills == null)
                diagnostics.Add(new ContentDiagnostic("StartingSkills", "시작 Skill 목록이 없다."));

            // 정의 자체가 잘못되면 참조 검증은 건너뛴다. 잘못된 수치 때문에
            // 생긴 가짜 '시작 Skill 없음' 진단을 보고하지 않는다.
            if (diagnostics.Count == 0)
                ContentInvariants.CollectSkills(definitions, startingSkills, diagnostics, out _);
            return diagnostics.Count == 0
                ? new SkillLoadResult(new SkillContent(definitions, startingSkills), diagnostics)
                : new SkillLoadResult(null, diagnostics);
        }

        private static PassiveSkillDefinition LoadSkill(SkillData item, string at, List<ContentDiagnostic> into)
        {
            try
            {
                switch (item.Kind)
                {
                    case "Breaker":
                        return new BreakerSkillDefinition(item.Id,
                            new BreakerStats(item.Radius, item.Interval, item.Damage));
                    case "PiercingLaser":
                        return new PiercingLaserDefinition(item.Id,
                            new PiercingLaserStats(item.Interval, item.Damage, item.Width, item.TelegraphDuration),
                            item.BoundaryRadius);
                    default:
                        into.Add(new ContentDiagnostic(at + ".Kind",
                            $"알 수 없는 Skill 종류 '{item.Kind}'. 가능한 값: Breaker, PiercingLaser."));
                        return null;
                }
            }
            catch (ArgumentException error)
            {
                into.Add(new ContentDiagnostic(at, error.Message));
                return null;
            }
        }
    }
}
