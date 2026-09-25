using System.Collections.Generic;
using BlackHole.Skills;
using UnityEngine;

namespace BlackHole.Unity
{
    [CreateAssetMenu(fileName = "EnemyCatalog", menuName = "BlackHole/Enemy Catalog")]
    public sealed class EnemyCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();

        public EnemyCatalog Load() => EnemyCatalog.Load(_enemies);
    }
}
