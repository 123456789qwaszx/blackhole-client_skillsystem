using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    public enum UpgradeResult { Purchased, UnknownNode, AlreadyPurchased, RequiresPreviousLevel, InBattle }

    // Player 한 명의 전투 밖 구매 기록. 게임 종료 뒤 저장할 데이터는 NodeId 목록이다.
    public sealed class SkillProgress
    {
        private readonly SkillCatalog _catalog;
        private readonly Dictionary<SkillType, int> _levels = new Dictionary<SkillType, int>();
        private readonly HashSet<string> _purchased = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> _nodeIds = new List<string>();

        public IReadOnlyList<string> PurchasedNodes => _nodeIds.AsReadOnly();
        public bool InBattle { get; private set; }

        public SkillProgress(SkillCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            foreach (SkillType skill in catalog.StartingSkills) _levels.Add(skill, 1);
        }

        public int Level(SkillType skill) => _levels.TryGetValue(skill, out int level) ? level : 0;

        public UpgradeResult Purchase(string nodeId)
        {
            if (InBattle) return UpgradeResult.InBattle;
            if (!_catalog.TryGetNode(nodeId, out UpgradeNode node)) return UpgradeResult.UnknownNode;
            if (_purchased.Contains(nodeId)) return UpgradeResult.AlreadyPurchased;
            if (node.Level != Level(node.Skill) + 1) return UpgradeResult.RequiresPreviousLevel;
            _levels[node.Skill] = node.Level;
            _purchased.Add(nodeId);
            _nodeIds.Add(nodeId);
            return UpgradeResult.Purchased;
        }

        // 스킬 실행은 이 스냅샷만 받는다. 이후 구매가 기존 전투에 소급 적용되지 않는다.
        public IReadOnlyDictionary<SkillType, UpgradeStat> Snapshot()
        {
            var stats = new Dictionary<SkillType, UpgradeStat>();
            foreach (KeyValuePair<SkillType, int> entry in _levels)
                stats.Add(entry.Key, _catalog.StatsAt(entry.Key, entry.Value));
            return stats;
        }

        public void BeginBattle()
        {
            if (InBattle) throw new InvalidOperationException("이미 전투 중이다.");
            InBattle = true;
        }

        public void EndBattle() => InBattle = false;
    }
}
