using System.Collections.Generic;
using BlackHole.Skills;
using UnityEngine;

namespace BlackHole.Unity
{
    [CreateAssetMenu(fileName = "SkillCatalog", menuName = "BlackHole/Skill Catalog")]
    public sealed class SkillCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<SkillData> _skills = new List<SkillData>();

        public SkillLoadResult Load() => SkillCatalog.Load(_skills);
    }
}
