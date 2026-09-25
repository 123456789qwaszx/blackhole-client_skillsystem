using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // gameplay.json의 Skills와 StartingSkills만으로 검증할 수 있는 스킬 정의 묶음.
    // 노드, 화면 배치, Player별 전투 상태를 소유하지 않는다.
    public sealed class SkillContent
    {
        private readonly Dictionary<string, PassiveSkillDefinition> _byId;

        public IReadOnlyList<PassiveSkillDefinition> Skills { get; }
        public IReadOnlyList<string> StartingSkills { get; }

        internal SkillContent(IReadOnlyList<PassiveSkillDefinition> skills, IReadOnlyList<string> startingSkills)
        {
            Skills = Array.AsReadOnly(Copy(skills));
            StartingSkills = Array.AsReadOnly(Copy(startingSkills));
            _byId = new Dictionary<string, PassiveSkillDefinition>(StringComparer.Ordinal);
            foreach (PassiveSkillDefinition skill in Skills) _byId.Add(skill.Id, skill);
        }

        public bool TryGetSkill(string id, out PassiveSkillDefinition skill)
        {
            skill = null;
            return id != null && _byId.TryGetValue(id, out skill);
        }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            var copy = new T[source.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = source[i];
            return copy;
        }
    }

    public sealed class SkillLoadResult
    {
        public SkillContent Content { get; }
        public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }
        public bool Succeeded => Content != null;

        internal SkillLoadResult(SkillContent content, List<ContentDiagnostic> diagnostics)
        {
            Content = content;
            Diagnostics = diagnostics.AsReadOnly();
        }
    }
}
