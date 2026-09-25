using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // Player 한 명의 이번 전투 구성. 구매 기록의 스냅샷이며 Skill 실행 상태를 소유하지 않는다.
    public sealed class PlayerLoadout
    {
        private readonly Dictionary<string, IReadOnlyList<StatModifier>> _modifiers;
        public IReadOnlyList<PassiveSkillDefinition> Skills { get; }

        private PlayerLoadout(List<PassiveSkillDefinition> skills,
            Dictionary<string, IReadOnlyList<StatModifier>> modifiers)
        {
            Skills = skills.AsReadOnly();
            _modifiers = modifiers;
        }

        public IReadOnlyList<StatModifier> ModifiersFor(string skillId) =>
            _modifiers.TryGetValue(skillId, out IReadOnlyList<StatModifier> values)
                ? values : Array.Empty<StatModifier>();

        // 툴의 구매 미리보기와 전투 조립이 같은 함수를 쓴다.
        public static PlayerLoadout Compile(PlayerState state, GameContent content)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (content == null) throw new ArgumentNullException(nameof(content));
            foreach (string id in state.Upgrades)
                if (!content.TryGetUpgrade(id, out _))
                    throw new InvalidOperationException($"구매한 노드 '{id}'가 콘텐츠에 없다.");

            var owned = new HashSet<string>(StringComparer.Ordinal);
            foreach (PassiveSkillDefinition skill in content.StartingSkills) owned.Add(skill.Id);
            var bySkill = new Dictionary<string, List<StatModifier>>(StringComparer.Ordinal);
            foreach (UpgradeNodeDefinition node in content.Upgrades)
            {
                if (!state.Owns(node.Id)) continue;
                foreach (UpgradeEffect effect in node.Effects)
                {
                    if (effect.Kind == UpgradeEffectKind.SkillUnlock) owned.Add(effect.Skill.Id);
                    if (effect.Kind != UpgradeEffectKind.SkillStat) continue;
                    string id = effect.Skill.Id;
                    if (!bySkill.TryGetValue(id, out List<StatModifier> list))
                        bySkill.Add(id, list = new List<StatModifier>());
                    list.Add(effect.Modifier.Value);
                }
            }

            var skills = new List<PassiveSkillDefinition>();
            var modifiers = new Dictionary<string, IReadOnlyList<StatModifier>>(StringComparer.Ordinal);
            // 실행 순서는 Skill 콘텐츠 순서다. 노드 파일 순서와 구매 순서는 관여하지 않는다.
            foreach (PassiveSkillDefinition skill in content.Skills)
            {
                if (!owned.Contains(skill.Id)) continue;
                skills.Add(skill);
                var list = bySkill.TryGetValue(skill.Id, out List<StatModifier> found) ? found : new List<StatModifier>();
                list.Sort((a, b) =>
                {
                    int id = string.CompareOrdinal(a.StatId, b.StatId);
                    if (id != 0) return id;
                    int operation = a.Operation.CompareTo(b.Operation);
                    return operation != 0 ? operation : a.Value.CompareTo(b.Value);
                });
                skill.ComputeStats(list); // 잘못된 최종 수치는 전투에 들어가기 전에 실패한다.
                modifiers.Add(skill.Id, list.AsReadOnly());
            }
            return new PlayerLoadout(skills, modifiers);
        }
    }
}
