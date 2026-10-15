using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class AccessibilitySegment : MonoBehaviour
    {
        public string id;
        public string displayName;
        public PedestrianNode start;
        public PedestrianNode end;
        [Min(.1f)] public float totalWidth = 2.4f;
        [Min(0)] public float freeWidth = 2.4f;
        public float crossSlopePercent = 1;
        [Range(0, 1)] public float roughness = .05f;
        [Min(0)] public float stepHeight;
        [Min(0)] public float surfaceAmplitude;
        [Min(.2f)] public float surfacePeriod = 2;
        public string pavement = "Concreto sintético";
        public bool hasAssessmentData = true;
        public bool networkOpen = true;
        public bool bidirectional = true;
        public VerificationState verification = VerificationState.Simulated;
        public string sourceFeatureId;
        [TextArea] public string geometryProvenance;
        public bool syntheticConnection;
        public bool elevationIsSimulated = true;
        public List<AccessibilityObstacle> obstacles = new List<AccessibilityObstacle>();
        public event Action Changed;

        public float HorizontalDistance => Vector2.Distance(
            new Vector2(start.Position.x, start.Position.z), new Vector2(end.Position.x, end.Position.z));
        public float Length => Vector3.Distance(start.Position, end.Position);
        public float SlopePercent => HorizontalDistance > .001f
            ? 100f * (end.Position.y - start.Position.y) / HorizontalDistance : 0f;
        public float EffectiveWidth
        {
            get
            {
                return ObstacleFootprints.FreeWidth(this);
            }
        }
        public float EffectiveStep
        {
            get
            {
                var step = stepHeight;
                foreach (var item in obstacles)
                    if (item != null && item.isActiveAndEnabled && item.kind == ObstacleKind.Step)
                        step = Mathf.Max(step, item.heightOrDepth);
                return step;
            }
        }
        public int RelevantObstacles
        {
            get
            {
                var count = 0;
                foreach (var item in obstacles) if (item != null && item.isActiveAndEnabled) count++;
                return count;
            }
        }
        public bool HasClosure
        {
            get
            {
                if (!networkOpen) return true;
                foreach (var item in obstacles)
                    if (item != null && item.isActiveAndEnabled && !item.canBypass &&
                        (item.blocksPassage || item.kind == ObstacleKind.Pothole)) return true;
                return false;
            }
        }
        public float EffectiveRoughness
        {
            get
            {
                float value = Mathf.Max(roughness, Mathf.Clamp01(surfaceAmplitude / .05f));
                foreach (var item in obstacles)
                    if (item != null && item.isActiveAndEnabled && item.kind == ObstacleKind.Pothole)
                        value = Mathf.Max(value, item.severity);
                return value;
            }
        }
        public void NotifyChanged() => Changed?.Invoke();
        private void OnValidate() => NotifyChanged();
    }

    public sealed class SegmentEvaluation
    {
        public AccessibilityRating rating;
        public string reason;
        public float cost;
        public bool Passable => rating == AccessibilityRating.Accessible || rating == AccessibilityRating.Difficult;
    }

    public static class AccessibilityEvaluator
    {
        public static SegmentEvaluation Evaluate(AccessibilitySegment s, AccessibilityProfile profile, bool forward)
        {
            if (s.HasClosure) return Result(AccessibilityRating.Inaccessible, "Passagem interditada.");
            if (!s.hasAssessmentData) return Result(AccessibilityRating.NotEvaluated, "Dados de acessibilidade ausentes.");
            if (s.HorizontalDistance < .001f) return Result(AccessibilityRating.Inaccessible, "Geometria degenerada.");
            if (s.EffectiveWidth < profile.requiredWidth) return Result(AccessibilityRating.Inaccessible, "Largura livre insuficiente para o perfil.");
            if (s.stepHeight > profile.maxStepHeight || ObstacleFootprints.StepBlocks(s, profile)) return Result(AccessibilityRating.Inaccessible, "Degrau excede a capacidade simulada e não há faixa de contorno suficiente.");
            float slope = s.SlopePercent * (forward ? 1 : -1);
            if (slope > profile.maxUphillPercent) return Result(AccessibilityRating.Inaccessible, "Subida excede a tolerância simulada.");
            if (-slope > profile.maxDownhillPercent) return Result(AccessibilityRating.Inaccessible, "Descida excede a tolerância simulada.");
            if (Mathf.Abs(s.crossSlopePercent) > profile.maxCrossSlopePercent) return Result(AccessibilityRating.Inaccessible, "Inclinação transversal excede a tolerância.");
            if (s.EffectiveRoughness > profile.maxRoughness) return Result(AccessibilityRating.Inaccessible, "Irregularidade excede a tolerância simulada.");
            var w = profile.weights;
            float narrow = Mathf.Max(0, profile.referenceFreeWidth - s.EffectiveWidth) / Mathf.Max(.1f, profile.referenceFreeWidth);
            float penalty = Mathf.Max(0, slope) * Mathf.Max(0, w.uphillPerPercent)
                + Mathf.Max(0, -slope) * Mathf.Max(0, w.downhillPerPercent)
                + Mathf.Abs(s.crossSlopePercent) * Mathf.Max(0, w.crossSlopePerPercent)
                + s.EffectiveRoughness * Mathf.Max(0, w.roughness)
                + narrow * Mathf.Max(0, w.narrowness) + s.RelevantObstacles * Mathf.Max(0, w.obstacle);
            bool difficult = narrow > 0 || s.RelevantObstacles > 0 || s.EffectiveStep > 0 || s.EffectiveRoughness > .2f ||
                slope > profile.maxUphillPercent * .65f || -slope > profile.maxDownhillPercent * .65f ||
                Mathf.Abs(s.crossSlopePercent) > profile.maxCrossSlopePercent * .65f;
            return new SegmentEvaluation { rating = difficult ? AccessibilityRating.Difficult : AccessibilityRating.Accessible,
                reason = difficult ? "Passável com esforço ou restrição no modelo de simulação." : "Passável pelos parâmetros simulados.",
                cost = s.Length * (1 + penalty) };
        }
        private static SegmentEvaluation Result(AccessibilityRating rating, string reason) =>
            new SegmentEvaluation { rating = rating, reason = reason, cost = float.PositiveInfinity };
    }
}
