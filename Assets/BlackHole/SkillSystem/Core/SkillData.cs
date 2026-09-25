using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    public enum SkillType { Breaker, PiercingLaser }

    // 스킬 실행 수치만 담는다. Unity JsonUtility도 읽을 수 있는 필드형 DTO다.
    [Serializable]
    public sealed class GameplayData
    {
        public int Version;
        public List<SkillData> Skills = new List<SkillData>();
    }

    [Serializable]
    public sealed class SkillData
    {
        public string Type;
        public SkillStats Stats;
    }

    [Serializable]
    public sealed class SkillStats
    {
        public float Damage;
        public float Interval;
        public float Radius;
        public float Width;
        public float TelegraphDuration;

        internal SkillStats Copy() => new SkillStats
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
