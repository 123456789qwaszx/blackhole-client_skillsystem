using System.Collections.Generic;
using BlackHole.Skills;
using UnityEngine;

namespace BlackHole.Unity
{
    [CreateAssetMenu(fileName = "SkillCatalog", menuName = "BlackHole/Skill Catalog")]
    public sealed class SkillCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<SkillData> _skills = new List<SkillData>();
        [SerializeField] private PlayerCombatStats _playerCombat = new PlayerCombatStats();

        public SkillLoadResult Load() => SkillCatalog.Load(_skills);

        public PlayerCombatStats LoadPlayerCombat()
        {
            if (_playerCombat == null || !_playerCombat.IsValid())
                throw new System.ArgumentException("Skill Catalog의 Player Combat 수치가 유효하지 않다.");
            return _playerCombat.Copy();
        }
    }
}
