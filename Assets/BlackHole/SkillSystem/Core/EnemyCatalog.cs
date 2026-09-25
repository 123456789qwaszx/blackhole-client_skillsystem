using System;
using System.Collections.Generic;

namespace BlackHole.Skills
{
    [Serializable]
    public sealed class EnemyDefinition
    {
        public string Id;
        public float Health;
        public DeathEffectDefinition DeathEffect;

        public EnemyDefinition Copy() => new EnemyDefinition
        {
            Id = Id,
            Health = Health,
            DeathEffect = DeathEffect?.Copy()
        };
    }

    public sealed class EnemyCatalog
    {
        private readonly Dictionary<string, EnemyDefinition> _definitions;

        private EnemyCatalog(Dictionary<string, EnemyDefinition> definitions) => _definitions = definitions;

        public EnemyDefinition Get(string id) => _definitions.TryGetValue(id, out EnemyDefinition definition)
            ? definition.Copy() : throw new ArgumentException($"정의되지 않은 Enemy: {id}", nameof(id));

        public static EnemyCatalog Load(IReadOnlyList<EnemyDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
                throw new ArgumentException("적 정의가 하나 이상 필요하다.", nameof(definitions));
            var byId = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Count; i++)
            {
                EnemyDefinition definition = definitions[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.Id) ||
                    float.IsNaN(definition.Health) || float.IsInfinity(definition.Health) ||
                    definition.Health <= 0 || definition.DeathEffect == null || !definition.DeathEffect.IsValid())
                    throw new ArgumentException($"Enemies[{i}]의 ID, HP 또는 사망 효과가 유효하지 않다.", nameof(definitions));
                if (byId.ContainsKey(definition.Id))
                    throw new ArgumentException($"Enemies[{i}].Id가 중복됐다: {definition.Id}", nameof(definitions));
                byId.Add(definition.Id, definition.Copy());
            }
            return new EnemyCatalog(byId);
        }
    }
}
