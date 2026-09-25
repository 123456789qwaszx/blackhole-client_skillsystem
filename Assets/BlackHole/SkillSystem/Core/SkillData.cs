using System;

namespace BlackHole.Skills
{
    public enum SkillType { Breaker, PiercingLaser }

    // Unity SO와 순수 C# 테스트가 공유하는 수치 데이터.
    [Serializable]
    public sealed class SkillData
    {
        public SkillType Type;
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
