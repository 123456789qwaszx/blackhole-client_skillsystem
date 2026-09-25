using System;
using System.Collections.Generic;

namespace BlackHole.Core
{
    // 트리 화면과 저작 도구의 표시 데이터. 구매나 Skill 계산에는 전달하지 않는다.
    [Serializable]
    public sealed class UpgradeNodeDisplay
    {
        public string NodeId;
        public int X;
        public int Y;
        public string Name;
        public string Description;
        public string Icon;
        public string Group;
    }

    [Serializable]
    public sealed class UpgradeLayout
    {
        public int Version = 1;
        public List<UpgradeNodeDisplay> Nodes = new List<UpgradeNodeDisplay>();

        // 저작 중에는 불완전한 파일도 열 수 있다. 오류를 모아서 도구에 돌려준다.
        public IReadOnlyList<ContentDiagnostic> Validate(GameContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var diagnostics = new List<ContentDiagnostic>();
            if (Version != 1) diagnostics.Add(new ContentDiagnostic("Layout.Version", "지원하는 버전은 1이다."));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var positions = new HashSet<(int, int)>();
            if (Nodes != null)
                for (int i = 0; i < Nodes.Count; i++)
                {
                    UpgradeNodeDisplay display = Nodes[i];
                    string at = $"Layout.Nodes[{i}]";
                    if (display == null || !content.TryGetUpgrade(display.NodeId, out _))
                    {
                        diagnostics.Add(new ContentDiagnostic(at + ".NodeId", "정의되지 않은 노드다."));
                        continue;
                    }
                    if (!ids.Add(display.NodeId)) diagnostics.Add(new ContentDiagnostic(at + ".NodeId", "중복 노드다."));
                    if (!positions.Add((display.X, display.Y))) diagnostics.Add(new ContentDiagnostic(at, "같은 칸에 노드가 겹친다."));
                }
            foreach (UpgradeNodeDefinition node in content.Upgrades)
                if (!ids.Contains(node.Id)) diagnostics.Add(new ContentDiagnostic($"Layout.Nodes[{node.Id}]", "표시 정보가 없다."));
            return diagnostics.AsReadOnly();
        }
    }
}
