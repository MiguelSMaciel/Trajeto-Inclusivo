using System;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class SimulationController : MonoBehaviour
    {
        public PedestrianGraph graph;
        public ScenarioManager scenarios;
        public RouteVisualizer visualizer;
        public AccessibilityProfile[] profiles;
        public int profileIndex = 1;
        public PedestrianNode origin, destination;
        public GeographicDatasetContext geographicContext;
        public RuntimeWorldEditor worldEditor{get;private set;}
        public RouteResult conventional { get; private set; }
        public RouteResult accessible { get; private set; }
        public RouteComparison comparison { get; private set; }
        public AccessibilitySegment selectedSegment;
        public bool showAccessible = true;
        public event Action RoutesChanged;
        private bool dirty;
        public AccessibilityProfile Profile => profiles[profileIndex];

        private void Start()
        {
            graph.Rebuild();
            foreach (var s in graph.segments) s.Changed += RequestRecalculation;
            scenarios.Changed += HandleScenarioChanged;
            AccessibilityProfile.ProfileChanged += RequestRecalculation;
            scenarios.CaptureBaseline(); scenarios.Apply(scenarios.currentIndex);
            worldEditor=GetComponent<RuntimeWorldEditor>()??gameObject.AddComponent<RuntimeWorldEditor>();worldEditor.Initialize(this);
            Recalculate();
        }
        private void LateUpdate() { if (dirty) Recalculate(); }
        public void RequestRecalculation() => dirty = true;
        private void HandleScenarioChanged()
        {
            if (geographicContext != null) geographicContext.ApplyAssessmentMode(graph);
            RequestRecalculation();
        }
        public void SetProfile(int index) { profileIndex = Mathf.Clamp(index, 0, profiles.Length - 1); RequestRecalculation(); }
        [ContextMenu("Recalcular rotas e geometria")]
        public void Recalculate()
        {
            dirty = false;
            graph.Rebuild();
            scenarios.map.RefreshGeometry();
            conventional = RouteCalculator.Calculate(graph, origin, destination, Profile, false);
            accessible = RouteCalculator.Calculate(graph, origin, destination, Profile, true);
            comparison=RouteComparison.Create(conventional,accessible,Profile);
            visualizer.Refresh(this);
            RoutesChanged?.Invoke();
        }
        private void OnDestroy()
        {
            if (graph != null) foreach (var s in graph.segments) if (s != null) s.Changed -= RequestRecalculation;
            if (scenarios != null) scenarios.Changed -= HandleScenarioChanged;
            AccessibilityProfile.ProfileChanged -= RequestRecalculation;
        }
    }
}
