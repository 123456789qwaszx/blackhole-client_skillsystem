using System.Collections.Generic;
using BlackHole.Skills;
using BlackHole.Skills.TestPack;
using UnityEngine;

namespace BlackHole.Unity
{
    // 코어가 확정한 공격·사망 효과 기록을 Game 뷰의 선과 원으로 그린다.
    public sealed class SkillEffectsView : MonoBehaviour
    {
        private const int MaxStrokes = 96;
        private const int CircleSegments = 48;
        private readonly List<Stroke> _strokes = new List<Stroke>();
        private readonly List<LineRenderer> _telegraphs = new List<LineRenderer>();
        private Material _material;

        private sealed class Stroke
        {
            public LineRenderer Line;
            public Color Color;
            public float Duration;
            public float Remaining;
            public float Width;
        }

        public void Render(SkillBattle battle, float delta)
        {
            AgeStrokes(delta);
            foreach (SkillVisual visual in battle.LastSkillVisuals)
            {
                if (visual.Kind == SkillVisualKind.BreakerPulse)
                    AddCircle(visual.Center, visual.Radius, new Color(0.2f, 0.95f, 0.5f), 0.32f, 0.09f);
                else
                    AddLine(visual.Start, visual.End, new Color(0.25f, 0.9f, 1f), 0.23f, visual.Width);
            }
            foreach (EffectActivation activation in battle.LastEffectActivations)
            {
                if (activation.Type == DeathEffectType.Explosion)
                    AddCircle(activation.Position, activation.Radius, new Color(1f, 0.35f, 0.12f), 0.4f, 0.12f);
                else if (activation.Type == DeathEffectType.AttackHaste)
                    AddCircle(activation.Position, 0.8f, new Color(0.3f, 1f, 0.45f), 0.55f, 0.12f);
                else if (activation.Type == DeathEffectType.GuaranteedCritical)
                    AddCircle(activation.Position, 0.8f, new Color(1f, 0.85f, 0.18f), 0.55f, 0.12f);
            }
            foreach (EffectHit hit in battle.LastEffectHits)
                if (hit.Type == DeathEffectType.ChainLightning)
                    AddLine(hit.From, hit.To, new Color(0.4f, 0.8f, 1f), 0.27f, 0.1f);
            foreach (SkillHit hit in battle.LastSkillHits)
                AddCircle(hit.Target.Position, hit.IsCritical ? 0.32f : 0.22f,
                    hit.IsCritical ? new Color(1f, 0.85f, 0.16f) : new Color(1f, 1f, 1f), 0.18f, 0.05f);
            RenderTelegraphs(battle);
        }

        public void Clear()
        {
            foreach (Stroke stroke in _strokes)
            {
                stroke.Remaining = 0;
                stroke.Line.enabled = false;
            }
            foreach (LineRenderer line in _telegraphs) line.enabled = false;
        }

        private void RenderTelegraphs(SkillBattle battle)
        {
            int count = 0;
            foreach (SkillRuntime skill in battle.Skills)
            {
                if (!battle.IsSkillEnabled(skill.Type) || !(skill is LaserRuntime laser)) continue;
                foreach (LaserShot shot in laser.PendingShots)
                {
                    if (count == _telegraphs.Count) _telegraphs.Add(CreateLine("Laser Telegraph"));
                    LineRenderer line = _telegraphs[count++];
                    line.enabled = true;
                    float progress = 1f - Mathf.Clamp01(shot.Remaining / laser.TelegraphDuration);
                    line.startColor = line.endColor = new Color(0.2f, 0.85f, 1f, 0.35f + 0.55f * progress);
                    line.startWidth = line.endWidth = 0.025f + 0.045f * progress;
                    line.positionCount = 2;
                    line.SetPosition(0, Position(shot.Start));
                    line.SetPosition(1, Position(shot.End));
                }
            }
            for (int i = count; i < _telegraphs.Count; i++) _telegraphs[i].enabled = false;
        }

        private void AddCircle(Point2 center, float radius, Color color, float duration, float width)
        {
            Stroke stroke = NextStroke();
            if (stroke == null) return;
            StartStroke(stroke, color, duration, width);
            stroke.Line.positionCount = CircleSegments + 1;
            for (int i = 0; i <= CircleSegments; i++)
            {
                float angle = i * (Mathf.PI * 2f / CircleSegments);
                stroke.Line.SetPosition(i, new Vector3(center.X + radius * Mathf.Cos(angle),
                    center.Y + radius * Mathf.Sin(angle), 0));
            }
        }

        private void AddLine(Point2 from, Point2 to, Color color, float duration, float width)
        {
            Stroke stroke = NextStroke();
            if (stroke == null) return;
            StartStroke(stroke, color, duration, width);
            stroke.Line.positionCount = 2;
            stroke.Line.SetPosition(0, Position(from));
            stroke.Line.SetPosition(1, Position(to));
        }

        private static Vector3 Position(Point2 point) => new Vector3(point.X, point.Y, 0);

        private void StartStroke(Stroke stroke, Color color, float duration, float width)
        {
            stroke.Color = color;
            stroke.Duration = duration;
            stroke.Remaining = duration;
            stroke.Width = width;
            stroke.Line.enabled = true;
            stroke.Line.startColor = stroke.Line.endColor = color;
            stroke.Line.startWidth = stroke.Line.endWidth = width;
        }

        private Stroke NextStroke()
        {
            foreach (Stroke stroke in _strokes)
                if (stroke.Remaining <= 0) return stroke;
            if (_strokes.Count >= MaxStrokes) return null;
            var created = new Stroke { Line = CreateLine("Combat Effect") };
            _strokes.Add(created);
            return created;
        }

        private void AgeStrokes(float delta)
        {
            foreach (Stroke stroke in _strokes)
            {
                if (stroke.Remaining <= 0) continue;
                stroke.Remaining = Mathf.Max(0, stroke.Remaining - delta);
                float strength = stroke.Remaining / stroke.Duration;
                Color faded = stroke.Color;
                faded.a *= strength;
                stroke.Line.startColor = stroke.Line.endColor = faded;
                stroke.Line.startWidth = stroke.Line.endWidth = stroke.Width * strength;
                if (stroke.Remaining == 0) stroke.Line.enabled = false;
            }
        }

        private LineRenderer CreateLine(string objectName)
        {
            if (_material == null) _material = new Material(Shader.Find("Sprites/Default"));
            var effect = new GameObject(objectName);
            effect.transform.SetParent(transform, false);
            var line = effect.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = 2;
            line.enabled = false;
            return line;
        }

        private void OnDestroy()
        {
            if (_material != null) Destroy(_material);
        }
    }
}
