using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    public readonly struct UpgradeEdge
    {
        public string A { get; }
        public string B { get; }
        public UpgradeEdge(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) || a == b)
                throw new ArgumentException("서로 다른 두 노드 ID가 필요하다.");
            A = a;
            B = b;
        }
    }

    // 무방향 그래프. 순환은 허용하고 시작점에서 닿지 않는 노드는 거부한다.
    // 좌표·아이콘은 이 규칙에 영향을 주지 않는다.
    public sealed class UpgradeGraph
    {
        private readonly Dictionary<string, HashSet<string>> _neighbors = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly HashSet<string> _starts;
        public IReadOnlyList<string> StartingNodes { get; }
        public IReadOnlyList<UpgradeEdge> Edges { get; }

        public UpgradeGraph(IReadOnlyList<UpgradeNodeDefinition> nodes,
            IReadOnlyList<string> startingNodes, IReadOnlyList<UpgradeEdge> edges)
        {
            if (nodes == null || startingNodes == null || edges == null) throw new ArgumentNullException();
            foreach (UpgradeNodeDefinition node in nodes)
            {
                if (node == null || _neighbors.ContainsKey(node.Id)) throw new ArgumentException("노드 ID가 중복되거나 비어 있다.");
                _neighbors.Add(node.Id, new HashSet<string>(StringComparer.Ordinal));
            }
            _starts = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in startingNodes)
                if (id == null || !_neighbors.ContainsKey(id) || !_starts.Add(id))
                    throw new ArgumentException($"시작 노드 '{id}'가 없거나 중복됐다.");
            var copy = new List<UpgradeEdge>();
            foreach (UpgradeEdge edge in edges)
            {
                if (edge.A == null || edge.B == null || edge.A == edge.B ||
                    !_neighbors.ContainsKey(edge.A) || !_neighbors.ContainsKey(edge.B))
                    throw new ArgumentException($"연결선 '{edge.A}'–'{edge.B}'의 노드가 없거나 자기 연결이다.");
                if (!_neighbors[edge.A].Add(edge.B)) throw new ArgumentException($"중복 연결선 '{edge.A}'–'{edge.B}'.");
                _neighbors[edge.B].Add(edge.A);
                copy.Add(edge);
            }
            StartingNodes = new List<string>(startingNodes).AsReadOnly();
            Edges = copy.AsReadOnly();
            HashSet<string> reached = ReachWithout(null);
            foreach (string id in _neighbors.Keys)
                if (!reached.Contains(id)) throw new ArgumentException($"시작 노드에서 닿지 않는 노드 '{id}'.");
        }

        public bool IsRevealed(string id, PlayerState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (id == null || !_neighbors.TryGetValue(id, out HashSet<string> neighbors)) return false;
            if (_starts.Contains(id) || state.Owns(id)) return true;
            foreach (string neighbor in neighbors)
                if (state.Owns(neighbor)) return true;
            return false;
        }

        // 해금 노드를 지워도 강화 노드에 닿으면, 해금을 우회해 살 수 있는 잘못된 경로다.
        internal bool RequiresUnlock(string nodeId, string unlockId) =>
            nodeId == unlockId || !ReachWithout(unlockId).Contains(nodeId);

        private HashSet<string> ReachWithout(string excluded)
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<string>();
            foreach (string start in _starts)
                if (start != excluded && reached.Add(start)) pending.Enqueue(start);
            while (pending.Count > 0)
            {
                foreach (string next in _neighbors[pending.Dequeue()])
                    if (next != excluded && reached.Add(next)) pending.Enqueue(next);
            }
            return reached;
        }
    }
}
