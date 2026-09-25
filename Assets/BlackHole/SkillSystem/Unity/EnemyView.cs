using BlackHole.Skills.TestPack;
using UnityEngine;

namespace BlackHole.Unity
{
    // 테스트 팩의 HP를 Game 뷰와 Inspector에 보여 주는 표시 전용 컴포넌트.
    public sealed class EnemyView : MonoBehaviour
    {
        [SerializeField] private float _health;
        private EnemyTarget _target;
        private SpriteRenderer _renderer;

        public void Bind(EnemyTarget target, Sprite sprite)
        {
            _target = target;
            transform.position = new Vector3(target.Position.X, target.Position.Y, 0);
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = 1;
            Refresh();
        }

        public void Refresh()
        {
            _health = _target.Health;
            _renderer.color = _target.IsAlive ? Color.red : Color.gray;
        }
    }
}
