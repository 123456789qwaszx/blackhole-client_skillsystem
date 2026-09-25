using BlackHole.Skills;
using BlackHole.Skills.TestPack;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHole.Unity
{
    // 샘플 씬의 진입점. 실제 게임 규칙이 아니라 스킬 발동과 적 피해를 눈으로 확인하는 샌드박스다.
    public sealed class GameHost : MonoBehaviour
    {
        private SkillCatalog _catalog;
        private SkillProgress _progress;
        private SkillBattle _battle;
        private Camera _camera;
        private string _message;

        private void Awake()
        {
            TextAsset json = Resources.Load<TextAsset>("gameplay");
            if (json == null) { Fail("Resources/gameplay.json이 없다."); return; }
            GameplayData raw = JsonUtility.FromJson<GameplayData>(json.text);
            SkillLoadResult result = SkillCatalog.Load(raw);
            if (!result.Succeeded)
            {
                foreach (SkillDiagnostic error in result.Diagnostics) Debug.LogError(error.ToString());
                Fail("gameplay.json 수치를 확인하세요.");
                return;
            }
            _catalog = result.Catalog;
            _progress = new SkillProgress(_catalog);
            _camera = Camera.main;
            if (_camera != null) { _camera.orthographic = true; _camera.orthographicSize = 11; }
            StartBattle();
        }

        private void Update()
        {
            if (_battle == null) return;
            if (_camera != null && Mouse.current != null)
            {
                Vector3 position = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                _battle.Aim = new Point2(position.x, position.y);
            }
            _battle.Advance(Time.deltaTime);
        }

        private void StartBattle()
        {
            _battle = new SkillBattle(_progress, 1, 9, new FixedRandom(0));
            _battle.AddEnemy(new Point2(0, 0), 40);
            _battle.AddEnemy(new Point2(2, 0), 40);
            _battle.AddEnemy(new Point2(0, 2), 40);
        }

        private void OnDisable() { _battle?.End(); }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 290, 370), GUI.skin.box);
            GUILayout.Label("Skill Sandbox / aim: mouse");
            if (_catalog == null) { GUILayout.Label(_message); GUILayout.EndArea(); return; }
            foreach (SkillType skill in (SkillType[])System.Enum.GetValues(typeof(SkillType)))
                GUILayout.Label($"{skill}: Lv {_progress.Level(skill)} / {_catalog.MaxLevel(skill)}");
            if (_battle != null)
            {
                foreach (EnemyTarget enemy in _battle.Enemies)
                    GUILayout.Label($"Enemy ({enemy.Position.X}, {enemy.Position.Y}) HP {enemy.Health:0.0}");
                if (GUILayout.Button("End battle")) { _battle.End(); _battle = null; }
            }
            else
            {
                foreach (var node in _catalog.Nodes)
                    if (GUILayout.Button("Buy " + node.Key))
                        _message = node.Key + ": " + _progress.Purchase(node.Key);
                if (GUILayout.Button("Start battle")) StartBattle();
            }
            GUILayout.Label(_message);
            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            if (_battle == null) return;
            foreach (EnemyTarget enemy in _battle.Enemies)
            {
                Gizmos.color = enemy.IsAlive ? Color.red : Color.gray;
                Gizmos.DrawSphere(new Vector3(enemy.Position.X, enemy.Position.Y, 0), 0.22f);
            }
            if (_battle.Aim.HasValue)
            {
                Point2 aim = _battle.Aim.Value;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(new Vector3(aim.X, aim.Y, 0), 0.12f);
            }
            foreach (SkillRuntime skill in _battle.Skills)
                if (skill is LaserRuntime laser)
                {
                    Gizmos.color = Color.yellow;
                    foreach (LaserShot shot in laser.PendingShots) DrawLine(shot);
                    if (laser.LastFired.HasValue) { Gizmos.color = Color.cyan; DrawLine(laser.LastFired.Value); }
                }
        }

        private static void DrawLine(LaserShot shot) =>
            Gizmos.DrawLine(new Vector3(shot.Start.X, shot.Start.Y, 0), new Vector3(shot.End.X, shot.End.Y, 0));

        private void Fail(string message) { _message = message; Debug.LogError(message); enabled = false; }
    }
}
