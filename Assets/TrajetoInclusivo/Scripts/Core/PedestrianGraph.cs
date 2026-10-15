using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class PedestrianGraph : MonoBehaviour
    {
        public List<PedestrianNode> nodes = new List<PedestrianNode>();
        public List<AccessibilitySegment> segments = new List<AccessibilitySegment>();
        private readonly Dictionary<PedestrianNode, List<GraphArc>> adjacency = new Dictionary<PedestrianNode, List<GraphArc>>();

        public void Rebuild()
        {
            adjacency.Clear();
            foreach (var n in nodes) if (n != null) adjacency[n] = new List<GraphArc>();
            foreach (var s in segments)
            {
                if (s == null || s.start == null || s.end == null || !adjacency.ContainsKey(s.start) || !adjacency.ContainsKey(s.end)) continue;
                adjacency[s.start].Add(new GraphArc(s.end, s, true));
                if (s.bidirectional) adjacency[s.end].Add(new GraphArc(s.start, s, false));
            }
        }
        public IEnumerable<GraphArc> Neighbors(PedestrianNode node) => adjacency.TryGetValue(node, out var arcs) ? arcs : System.Array.Empty<GraphArc>();
    }

    public readonly struct GraphArc
    {
        public readonly PedestrianNode to;
        public readonly AccessibilitySegment segment;
        public readonly bool forward;
        public GraphArc(PedestrianNode to, AccessibilitySegment segment, bool forward)
        { this.to = to; this.segment = segment; this.forward = forward; }
    }
}
