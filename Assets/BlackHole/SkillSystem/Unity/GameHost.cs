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
        [SerializeField] private SkillCatalogAsset _skillCatalog;
        [SerializeField] private EnemyCatalogAsset _enemyCatalog;
        [SerializeField, Min(0.1f)] private float _arenaRadius = 9;
        [SerializeField, Range(20, 40)] private int _consoleFontSize = 26;

        private readonly List<EnemyView> _enemyViews = new List<EnemyView>();
        private readonly Dictionary<SkillType, bool> _enabledBySkill = new Dictionary<SkillType, bool>();
        private SkillCatalog _catalog;
        private EnemyCatalog _enemies;
        private SkillBattle _battle;
        private Camera _camera;
        private string _message;
        private Sprite _enemySprite;
        private Texture2D _enemyTexture;
        private GUISkin _consoleSkin;
        private Texture2D _statusCircle;
        private int _styledFontSize;
        private Vector2 _skillScroll;
        private Vector2 _detailsScroll;
        private SkillType? _selectedSkill;

        private void Awake()
        {
            if (_skillCatalog == null) { Fail("GameHost에 Skill Catalog SO가 연결되지 않았다."); return; }
            SkillLoadResult result = _skillCatalog.Load();
            if (!result.Succeeded)
            {
                foreach (SkillDiagnostic error in result.Diagnostics) Debug.LogError(error.ToString());
                Fail("Skill Catalog SO의 수치를 확인하세요.");
                return;
            }
            _catalog = result.Catalog;
            if (_enemyCatalog == null) { Fail("GameHost에 Enemy Catalog SO가 연결되지 않았다."); return; }
            try { _enemies = _enemyCatalog.Load(); }
            catch (System.ArgumentException error) { Fail(error.Message); return; }
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
            AddEnemy(new Point2(0, 0), "normal");
            AddEnemy(new Point2(-2, 0), "electric");
            AddEnemy(new Point2(-2, -2), "explosive");
            AddEnemy(new Point2(0, 2), "haste");
            AddEnemy(new Point2(2, 2), "critical");
        }

        private void AddEnemy(Point2 position, string id)
        {
            EnemyTarget enemy = _battle.AddEnemy(position, _enemies.Get(id));
            var gameObject = new GameObject($"Enemy {id} ({position.X}, {position.Y})");
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
            if (_statusCircle != null) Destroy(_statusCircle);
            if (_consoleSkin != null) Destroy(_consoleSkin);
        }

        private void OnGUI()
        {
            ApplyConsoleStyle();
            GUISkin previous = GUI.skin;
            GUI.skin = _consoleSkin;
            float width = Mathf.Min(400, (Screen.width - 36) * 0.42f);
            float skillHeight = Mathf.Min(250, Screen.height * 0.48f);
            DrawSkillConsole(new Rect(12, 12, width, skillHeight));
            if (_selectedSkill.HasValue && _catalog != null)
                DrawSkillDetails(new Rect(12, 20 + skillHeight, width,
                    Mathf.Max(0, Screen.height - skillHeight - 32)));
            float enemyHeight = 32 + _consoleFontSize + 20 +
                (_battle?.Enemies.Count ?? 0) * (_consoleFontSize * 2 + 16);
            DrawEnemyConsole(new Rect(Screen.width - width - 12, 12, width,
                Mathf.Min(enemyHeight, Screen.height - 24)));
            GUI.skin = previous;
        }

        private void DrawSkillConsole(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            _skillScroll = GUILayout.BeginScrollView(_skillScroll);
            GUILayout.Label("Skill Sandbox / aim: mouse");
            if (_catalog != null)
                DrawSkillControls();
            if (_battle != null)
            {
                if (_battle.Buffs.HasteRemaining(1) > 0)
                    GUILayout.Label($"Attack haste: {_battle.Buffs.HasteRemaining(1):0.0} s");
                if (_battle.Buffs.CriticalRemaining(1) > 0)
                    GUILayout.Label($"Guaranteed critical: {_battle.Buffs.CriticalRemaining(1):0.0} s");
            }
            GUILayout.Label(_message);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawEnemyConsole(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            if (_battle != null && GUILayout.Button("Restart test"))
            {
                _battle.End();
                StartBattle();
            }
            if (_battle != null)
            {
                IReadOnlyList<EnemyTarget> enemies = _battle.Enemies;
                for (int i = 0; i < enemies.Count; i++)
                {
                    EnemyTarget enemy = enemies[i];
                    GUILayout.Label($"{enemy.Id}: HP {enemy.Health:0.0} / {enemy.MaxHealth:0.0}");
                    GUILayout.Space(8);
                }
            }
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
            if (_statusCircle == null) _statusCircle = CreateStatusCircle();
            foreach (SkillType skill in _catalog.AvailableSkills)
            {
                GUILayout.BeginHorizontal();
                Rect button = GUILayoutUtility.GetRect(44, 44, GUILayout.Width(44), GUILayout.Height(44));
                bool enabled = _enabledBySkill[skill];
                if (GUI.Button(button, GUIContent.none, GUIStyle.none))
                {
                    enabled = !enabled;
                    _enabledBySkill[skill] = enabled;
                    _battle?.SetSkillEnabled(skill, enabled);
                }
                Color previous = GUI.color;
                GUI.color = enabled ? new Color(0.25f, 0.8f, 0.35f) : new Color(0.45f, 0.45f, 0.45f);
                GUI.DrawTexture(new Rect(button.x + 6, button.y + 6, 32, 32), _statusCircle);
                GUI.color = previous;
                if (GUILayout.Button(skill.ToString(), GUILayout.ExpandWidth(true)))
                    _selectedSkill = skill;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawSkillDetails(Rect area)
        {
            GUILayout.BeginArea(area, GUI.skin.box);
            _detailsScroll = GUILayout.BeginScrollView(_detailsScroll);
            SkillType skill = _selectedSkill.Value;
            SkillStats stats = _catalog.StatsFor(skill);
            GUILayout.Label(skill.ToString() + " / Stats");
            GUILayout.Label($"Damage: {stats.Damage:0.##}");
            GUILayout.Label($"Interval: {stats.Interval:0.##} s");
            if (skill == SkillType.Breaker)
                GUILayout.Label($"Radius: {stats.Radius:0.##}");
            else if (skill == SkillType.PiercingLaser)
            {
                GUILayout.Label($"Width: {stats.Width:0.##}");
                GUILayout.Label($"Telegraph: {stats.TelegraphDuration:0.##} s");
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static Texture2D CreateStatusCircle()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size / 2f;
                    float dy = y + 0.5f - size / 2f;
                    texture.SetPixel(x, y, dx * dx + dy * dy <= 196 ? Color.white : Color.clear);
                }
            texture.Apply();
            return texture;
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
            foreach (EffectActivation effect in _battle.LastEffectActivations)
                if (effect.Type == DeathEffectType.Explosion)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(new Vector3(effect.Position.X, effect.Position.Y, 0), effect.Radius);
                }
            foreach (EffectHit hit in _battle.LastEffectHits)
                if (hit.Type == DeathEffectType.ChainLightning)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(new Vector3(hit.From.X, hit.From.Y, 0),
                        new Vector3(hit.To.X, hit.To.Y, 0));
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
