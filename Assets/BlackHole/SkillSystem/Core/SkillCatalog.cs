using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BlackHole.Skills
{
    public readonly struct UpgradeNode
    {
        public SkillType Skill { get; }
        public int Level { get; }
        public UpgradeNode(SkillType skill, int level) { Skill = skill; Level = level; }
    }

    // rawData를 한 번 검증해 SkillType → 레벨별 수치, NodeId → 다음 레벨로 색인한다.
    public sealed class SkillCatalog
    {
        private readonly Dictionary<SkillType, UpgradeStat[]> _stats;
        private readonly Dictionary<string, UpgradeNode> _nodes;
        private readonly SkillType[] _starting;

        public IReadOnlyList<SkillType> AvailableSkills { get; }
        public IReadOnlyList<SkillType> StartingSkills => Array.AsReadOnly(_starting);
        public IReadOnlyDictionary<string, UpgradeNode> Nodes => new ReadOnlyDictionary<string, UpgradeNode>(_nodes);

        private SkillCatalog(Dictionary<SkillType, UpgradeStat[]> stats,
            Dictionary<string, UpgradeNode> nodes, SkillType[] starting)
        {
            _stats = stats;
            _nodes = nodes;
            _starting = starting;
            var available = new List<SkillType>(stats.Keys);
            available.Sort();
            AvailableSkills = available.AsReadOnly();
        }

        public int MaxLevel(SkillType skill) => _stats[skill].Length;

        public UpgradeStat StatsAt(SkillType skill, int level)
        {
            if (!_stats.TryGetValue(skill, out UpgradeStat[] levels))
                throw new ArgumentException($"정의되지 않은 Skill: {skill}", nameof(skill));
            if (level < 1 || level > levels.Length)
                throw new ArgumentOutOfRangeException(nameof(level));
            return levels[level - 1].Copy();
        }

        public bool TryGetNode(string nodeId, out UpgradeNode node)
        {
            node = default;
            return nodeId != null && _nodes.TryGetValue(nodeId, out node);
        }

        public static SkillLoadResult Load(GameplayData data)
        {
            var errors = new List<SkillDiagnostic>();
            var stats = new Dictionary<SkillType, UpgradeStat[]>();
            var nodes = new Dictionary<string, UpgradeNode>(StringComparer.Ordinal);
            var starting = new List<SkillType>();

            if (data == null)
                return new SkillLoadResult(null, new[] { new SkillDiagnostic("Gameplay", "데이터가 없다.") });
            if (data.Version != 1)
                errors.Add(new SkillDiagnostic("Version", "지원하는 버전은 1이다."));

            if (data.Skills == null)
                errors.Add(new SkillDiagnostic("Skills", "목록이 없다."));
            else for (int i = 0; i < data.Skills.Count; i++)
            {
                SkillData item = data.Skills[i];
                string path = $"Skills[{i}]";
                if (item == null || !TryType(item.Type, out SkillType type))
                {
                    errors.Add(new SkillDiagnostic(path + ".Type", "지원하지 않는 스킬 종류다."));
                    continue;
                }
                if (stats.ContainsKey(type))
                {
                    errors.Add(new SkillDiagnostic(path + ".Type", "스킬 종류가 중복됐다."));
                    continue;
                }
                if (item.Levels == null || item.Levels.Count == 0)
                {
                    errors.Add(new SkillDiagnostic(path + ".Levels", "Lv1 이상의 수치가 필요하다."));
                    continue;
                }
                var levels = new UpgradeStat[item.Levels.Count];
                for (int level = 0; level < levels.Length; level++)
                {
                    UpgradeStat value = item.Levels[level];
                    if (!Valid(type, value))
                        errors.Add(new SkillDiagnostic($"{path}.Levels[{level}]", "종류에 맞는 유한한 양수 수치가 필요하다."));
                    else
                        levels[level] = value.Copy();
                }
                stats.Add(type, levels);
            }

            if (data.StartingSkills == null)
                errors.Add(new SkillDiagnostic("StartingSkills", "목록이 없다."));
            else for (int i = 0; i < data.StartingSkills.Count; i++)
            {
                if (!TryType(data.StartingSkills[i], out SkillType type) || !stats.ContainsKey(type))
                    errors.Add(new SkillDiagnostic($"StartingSkills[{i}]", "정의되지 않은 스킬이다."));
                else if (starting.Contains(type))
                    errors.Add(new SkillDiagnostic($"StartingSkills[{i}]", "시작 스킬이 중복됐다."));
                else
                    starting.Add(type);
            }

            if (data.Upgrades == null)
                errors.Add(new SkillDiagnostic("Upgrades", "목록이 없다."));
            else for (int i = 0; i < data.Upgrades.Count; i++)
            {
                UpgradeNodeData item = data.Upgrades[i];
                string path = $"Upgrades[{i}]";
                if (item == null || string.IsNullOrWhiteSpace(item.Id) || nodes.ContainsKey(item.Id))
                {
                    errors.Add(new SkillDiagnostic(path + ".Id", "NodeId가 비었거나 중복됐다."));
                    continue;
                }
                if (!TryType(item.Skill, out SkillType type) || !stats.ContainsKey(type))
                {
                    errors.Add(new SkillDiagnostic(path + ".Skill", "정의되지 않은 스킬이다."));
                    continue;
                }
                if (item.Level < 1 || item.Level > stats[type].Length)
                {
                    errors.Add(new SkillDiagnostic(path + ".Level", "정의된 스킬 레벨이 아니다."));
                    continue;
                }
                nodes.Add(item.Id, new UpgradeNode(type, item.Level));
            }

            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, UpgradeNode> pair in nodes)
                if (!occupied.Add(pair.Value.Skill + "/" + pair.Value.Level))
                    errors.Add(new SkillDiagnostic("Upgrades[" + pair.Key + "]", "동일한 스킬 레벨을 여는 노드가 중복됐다."));

            return errors.Count == 0
                ? new SkillLoadResult(new SkillCatalog(stats, nodes, starting.ToArray()), errors)
                : new SkillLoadResult(null, errors);
        }

        private static bool TryType(string name, out SkillType type) =>
            Enum.TryParse(name, out type) && type.ToString() == name;

        private static bool Positive(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value > 0;
        private static bool Valid(SkillType type, UpgradeStat value)
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
