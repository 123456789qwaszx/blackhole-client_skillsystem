using System.Collections.Generic;
using BlackHole.Skills;
using BlackHole.Skills.TestPack;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHole.Unity
{
    // 샘플 씬의 진입점. 실제 게임 규칙이 아니라 스킬 발동과 적 피해를 눈으로 확인하는 샌드박스다.
    public sealed class GameHost : MonoBehaviour
    {
        [SerializeField] private TextAsset _gameplay;
        [SerializeField, Min(0.1f)] private float _arenaRadius = 9;
        [SerializeField, Min(0.1f)] private float _enemyHealth = 40;
        [SerializeField, Range(20, 40)] private int _consoleFontSize = 26;

        private readonly List<EnemyView> _enemyViews = new List<EnemyView>();
        private readonly Dictionary<SkillType, bool> _enabledBySkill = new Dictionary<SkillType, bool>();
        private SkillCatalog _catalog;
        private SkillBattle _battle;
        private Camera _camera;
        private string _message;
        private Sprite _enemySprite;
        private Texture2D _enemyTexture;
        private GUISkin _consoleSkin;
        private int _styledFontSize;
        private Vector2 _skillScroll;
        private Vector2 _enemyScroll;

        private void Awake()
        {
            TextAsset json = _gameplay != null ? _gameplay : Resources.Load<TextAsset>("gameplay");
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
            foreach (SkillType skill in _catalog.AvailableSkills) _enabledBySkill.Add(skill, true);
            _camera = Camera.main;
            if (_camera != null) { _camera.orthographic = true; _camera.orthographicSize = 11; }
            _enemySprite = CreateEnemySprite();
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
            foreach (EnemyView view in _enemyViews) view.Refresh();
        }

        private void StartBattle()
        {
            ClearEnemies();
            _battle = new SkillBattle(_catalog, 1, _arenaRadius, new FixedRandom(0));
            foreach (var choice in _enabledBySkill)
                if (!choice.Value) _battle.SetSkillEnabled(choice.Key, false);
            AddEnemy(new Point2(0, 0));
            AddEnemy(new Point2(2, 0));
            AddEnemy(new Point2(0, 2));
        }

        private void AddEnemy(Point2 position)
        {
            EnemyTarget enemy = _battle.AddEnemy(position, _enemyHealth);
            var gameObject = new GameObject($"Enemy ({position.X}, {position.Y})");
            gameObject.transform.SetParent(transform);
            var view = gameObject.AddComponent<EnemyView>();
            view.Bind(enemy, _enemySprite);
            _enemyViews.Add(view);
        }

        private void ClearEnemies()
        {
            foreach (EnemyView view in _enemyViews)
                if (view != null) Destroy(view.gameObject);
            _enemyViews.Clear();
        }

        private void OnDisable()
        {
            _battle?.End();
            _battle = null;
            ClearEnemies();
        }

        private void OnEnable()
        {
            if (_catalog != null && _battle == null) StartBattle();
        }

        private void OnDestroy()
        {
            if (_enemySprite != null) Destroy(_enemySprite);
            if (_enemyTexture != null) Destroy(_enemyTexture);
            if (_consoleSkin != null) Destroy(_consoleSkin);
        }

        private void OnGUI()
        {
            ApplyConsoleStyle();
            GUISkin previous = GUI.skin;
            GUI.skin = _consoleSkin;
            float width = Mathf.Min(400, (Screen.width - 36) * 0.42f);
            DrawSkillConsole(new Rect(12, 12, width, Mathf.Min(280, Screen.height * 0.65f)));
            DrawEnemyConsole(new Rect(Screen.width - width - 12, 12, width,
                Mathf.Min(300, Screen.height * 0.6f)));
            GUI.skin = previous;
        }

        private void DrawSkillConsole(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            _skillScroll = GUILayout.BeginScrollView(_skillScroll);
            GUILayout.Label("Skill Sandbox / aim: mouse");
            if (_catalog != null)
            {
                DrawSkillControls();
                if (_battle != null && GUILayout.Button("Restart test"))
                {
                    _battle.End();
                    StartBattle();
                }
            }
            GUILayout.Label(_message);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawEnemyConsole(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            _enemyScroll = GUILayout.BeginScrollView(_enemyScroll);
            GUILayout.Label("Enemy HP");
            if (_battle != null)
            {
                IReadOnlyList<EnemyTarget> enemies = _battle.Enemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyTarget enemy = enemies[i];
                    GUILayout.Label($"Enemy {i + 1}");
                    GUILayout.Label($"HP {enemy.Health:0.0} / {enemy.MaxHealth:0.0}");
                    GUILayout.Space(8);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void ApplyConsoleStyle()
        {
            if (_consoleSkin == null) _consoleSkin = Instantiate(GUI.skin);
            if (_styledFontSize == _consoleFontSize) return;
            _styledFontSize = _consoleFontSize;
            _consoleSkin.label.fontSize = _consoleFontSize;
            _consoleSkin.label.wordWrap = true;
            _consoleSkin.toggle.fontSize = _consoleFontSize;
            _consoleSkin.toggle.fixedHeight = _consoleFontSize + 18;
            _consoleSkin.button.fontSize = _consoleFontSize;
            _consoleSkin.button.fixedHeight = _consoleFontSize + 20;
            _consoleSkin.button.wordWrap = true;
        }

        private void DrawSkillControls()
        {
            GUILayout.Label("Skills (checked = active)");
            foreach (SkillType skill in _catalog.AvailableSkills)
            {
                bool selected = GUILayout.Toggle(_enabledBySkill[skill], skill.ToString());
                if (selected == _enabledBySkill[skill]) continue;
                _enabledBySkill[skill] = selected;
                _battle?.SetSkillEnabled(skill, selected);
            }
        }

        private void OnDrawGizmos()
        {
            if (_battle == null) return;
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

        private Sprite CreateEnemySprite()
        {
            const int size = 32;
            _enemyTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _enemyTexture.filterMode = FilterMode.Point;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size / 2f;
                    float dy = y + 0.5f - size / 2f;
                    _enemyTexture.SetPixel(x, y, dx * dx + dy * dy <= 196 ? Color.white : Color.clear);
                }
            _enemyTexture.Apply();
            return Sprite.Create(_enemyTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void Fail(string message) { _message = message; Debug.LogError(message); enabled = false; }
    }
}
