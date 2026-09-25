using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    public enum SkillType { Breaker, PiercingLaser }

    // gameplay.json의 스킬 부분. Unity JsonUtility도 읽을 수 있는 필드형 DTO다.
    [Serializable]
    public sealed class GameplayData
    {
        public int Version;
        public List<SkillData> Skills = new List<SkillData>();
        public List<string> StartingSkills = new List<string>();
        public List<UpgradeNodeData> Upgrades = new List<UpgradeNodeData>();
    }

    [Serializable]
    public sealed class SkillData
    {
        public string Type;
        // 배열의 첫 항목은 Lv1, 다음 항목은 Lv2다. 0은 미획득이다.
        public List<UpgradeStat> Levels = new List<UpgradeStat>();
    }

    [Serializable]
    public sealed class UpgradeNodeData
    {
        public string Id;
        public string Skill;
        public int Level;
    }

    // 레벨마다 완성된 수치. 증가량이나 런타임 수식은 JSON에 넣지 않는다.
    [Serializable]
    public sealed class UpgradeStat
    {
        public float Damage;
        public float Interval;
        public float Radius;
        public float Width;
        public float TelegraphDuration;

        internal UpgradeStat Copy() => new UpgradeStat
        {
            Damage = Damage, Interval = Interval, Radius = Radius,
            Width = Width, TelegraphDuration = TelegraphDuration
        };
    }

    public sealed class SkillDiagnostic
    {
        public string Path { get; }
        public string Message { get; }

        public SkillDiagnostic(string path, string message)
        {
            Path = path;
            Message = message;
        }

        public override string ToString() => $"{Path}: {Message}";
    }
}
