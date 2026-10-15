using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class ScenarioManager : MonoBehaviour
    {
        public PedestrianGraph graph;
        public MapGenerator map;
        public ScenarioDefinition[] scenarios;
        public int currentIndex;
        public event Action Changed;
        private readonly Dictionary<PedestrianNode, Vector3> basePositions = new Dictionary<PedestrianNode, Vector3>();
        private readonly Dictionary<PedestrianNode,(VerificationState state,string source)> elevationSources=new Dictionary<PedestrianNode,(VerificationState,string)>();
        private readonly Dictionary<AccessibilitySegment, (float total,float width, float roughness, float step, float cross,float amplitude,float period, bool data, bool open)> baseline =
            new Dictionary<AccessibilitySegment, (float,float, float, float, float,float,float, bool, bool)>();

        public void CaptureBaseline()
        {
            basePositions.Clear(); baseline.Clear();elevationSources.Clear();
            foreach (var n in graph.nodes){basePositions[n]=n.Position;elevationSources[n]=(n.elevationVerification,n.elevationSource);}
            foreach (var s in graph.segments) baseline[s] = (s.totalWidth,s.freeWidth,s.roughness,s.stepHeight,s.crossSlopePercent,s.surfaceAmplitude,s.surfacePeriod,s.hasAssessmentData,s.networkOpen);
        }
        public void Apply(int index)
        {
            if (scenarios == null || scenarios.Length == 0) return;
            if (basePositions.Count == 0) CaptureBaseline();
            currentIndex = Mathf.Clamp(index, 0, scenarios.Length - 1);
            foreach (var s in graph.segments)
            {
                foreach (var o in s.obstacles.ToArray())
                    if (o != null) { o.gameObject.SetActive(false); Destroy(o.gameObject); }
                s.obstacles.Clear();
                var b = baseline[s];
                s.totalWidth=b.total;s.freeWidth = b.width; s.roughness = b.roughness; s.stepHeight = b.step;
                s.surfaceAmplitude=b.amplitude;s.surfacePeriod=b.period;
                s.crossSlopePercent = b.cross; s.hasAssessmentData = b.data; s.networkOpen = b.open;
            }
            foreach (var pair in basePositions){pair.Key.transform.position=pair.Value;pair.Key.elevationVerification=elevationSources[pair.Key].state;pair.Key.elevationSource=elevationSources[pair.Key].source;}
            var scenario = scenarios[currentIndex];
            foreach (var node in graph.nodes)
                if (node.id == scenario.raisedNodeId && scenario.elevationOffset!=0){node.transform.position+=Vector3.up*scenario.elevationOffset;node.elevationVerification=VerificationState.Simulated;node.elevationSource="Alteração artificial de cenário";}
            foreach(var s in graph.segments)s.elevationIsSimulated=s.start.elevationVerification==VerificationState.Simulated || s.end.elevationVerification==VerificationState.Simulated;
            foreach (var spec in scenario.obstacles)
            {
                var segment = graph.segments.Find(s => s.id == spec.segmentId);
                if (segment == null) { Debug.LogWarning("Cenário referencia trecho inexistente: " + spec.segmentId); continue; }
                var o = CreateObstacle(segment, spec.kind);
                o.heightOrDepth = spec.heightOrDepth; o.remainingFreeWidth = spec.remainingFreeWidth;
                o.length = spec.length; o.severity = spec.severity;
                o.canBypass = spec.canBypass; o.blocksPassage = spec.blocksPassage;
                o.occupiedWidth = Mathf.Max(.1f, segment.totalWidth - spec.remainingFreeWidth);
            }
            map.RefreshGeometry();
            Changed?.Invoke();
        }
        public AccessibilityObstacle CreateObstacle(AccessibilitySegment segment, ObstacleKind kind)
        {
            var go = new GameObject("SIMULADO — " + kind);
            go.transform.SetParent(segment.transform, false);
            var obstacle = go.AddComponent<AccessibilityObstacle>();
            obstacle.Attach(segment); obstacle.kind = kind;
            obstacle.remainingFreeWidth = Mathf.Max(0, segment.totalWidth - obstacle.occupiedWidth);
            return obstacle;
        }
        public void RemoveObstacle(AccessibilityObstacle obstacle)
        {
            if (obstacle == null) return;
            var s = obstacle.segment;
            s.obstacles.Remove(obstacle); obstacle.gameObject.SetActive(false); Destroy(obstacle.gameObject);
            s.NotifyChanged();
        }
        public float BaseElevation(PedestrianNode node)=>basePositions.TryGetValue(node,out var p)?p.y:node.Position.y;
        public (VerificationState state,string source) BaseElevationSource(PedestrianNode node)=>elevationSources.TryGetValue(node,out var source)?source:(node.elevationVerification,node.elevationSource);
        public void SetBaseElevation(PedestrianNode node,float y,VerificationState state,string source)
        {
            var position=basePositions.TryGetValue(node,out var p)?p:node.Position;position.y=y;basePositions[node]=position;elevationSources[node]=(state,source);
        }
    }
}
