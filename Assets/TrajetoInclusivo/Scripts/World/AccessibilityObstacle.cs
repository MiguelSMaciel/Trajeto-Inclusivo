using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class AccessibilityObstacle : MonoBehaviour
    {
        public AccessibilitySegment segment;
        public ObstacleKind kind = ObstacleKind.Generic;
        public VerificationState verification = VerificationState.Simulated;
        [Range(0, 1)] public float positionAlongSegment = .5f;
        [Min(.05f)] public float length = 1.2f;
        [Min(.05f)] public float occupiedWidth = .8f;
        [Min(0)] public float remainingFreeWidth = 1.6f;
        [Min(0)] public float heightOrDepth = .15f;
        [Range(0, 1)] public float severity = .35f;
        public bool canBypass = true;
        public bool blocksPassage;
        public bool useFootprintForClearance;
        public float lateralOffset;
        [HideInInspector] public Transform visual;
        [System.NonSerialized] public Mesh generatedGeometry;
        [System.NonSerialized] public int generatedGeometryHash;

        public void Attach(AccessibilitySegment target)
        {
            if (segment != null) segment.obstacles.Remove(this);
            segment = target;
            if (segment != null && !segment.obstacles.Contains(this)) segment.obstacles.Add(this);
        }
        public void NotifyChanged() { if (segment != null) segment.NotifyChanged(); }
        private void OnValidate() => NotifyChanged();
        private void OnDestroy()
        {
            if(generatedGeometry!=null){if(Application.isPlaying)Destroy(generatedGeometry);else DestroyImmediate(generatedGeometry);}
            if (segment != null) { segment.obstacles.Remove(this); segment.NotifyChanged(); }
        }
    }
}
