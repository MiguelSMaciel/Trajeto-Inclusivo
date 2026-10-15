using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrajetoInclusivo
{
    public enum RouteFailure { None, NoConnection, Blocked, InsufficientData }

    public sealed class RouteResult
    {
        public readonly List<PedestrianNode> nodes = new List<PedestrianNode>();
        public readonly List<GraphArc> arcs = new List<GraphArc>();
        public RouteFailure failure;
        public string explanation;
        public float distance, ascent, maxSlope, cost;
        public int obstacleCount, problematicCount;
        public bool Found => failure == RouteFailure.None && nodes.Count > 0;
    }

    public static class RouteCalculator
    {
        // Dijkstra with a binary heap, O((V + E) log V), for the imported local network.
        // Conventional routing honors network closures, but ignores individual accessibility.
        public static RouteResult Calculate(PedestrianGraph graph, PedestrianNode origin, PedestrianNode destination,
            AccessibilityProfile profile, bool accessible)
        {
            var result = Search(graph, origin, destination, profile, accessible, false, false);
            if (result.Found) return result;
            if (!Search(graph, origin, destination, profile, false, true, false).Found)
            {
                result.failure = RouteFailure.NoConnection;
                result.explanation = "Origem e destino sem conexão na topologia do grafo.";
            }
            else if (accessible && Search(graph, origin, destination, profile, true, false, true).Found)
            {
                result.failure = RouteFailure.InsufficientData;
                result.explanation = "Uma ligação depende de trechos não avaliados; não é possível confirmar uma rota acessível.";
            }
            else
            {
                result.failure = RouteFailure.Blocked;
                result.explanation = accessible ? "Barreiras, largura ou inclinações impedem uma rota para este perfil." : "A rede está conectada, mas as passagens estão interditadas.";
            }
            return result;
        }

        private static RouteResult Search(PedestrianGraph graph, PedestrianNode origin, PedestrianNode destination,
            AccessibilityProfile profile, bool accessible, bool ignoreClosures, bool allowUnknown)
        {
            var result = new RouteResult { failure = RouteFailure.NoConnection };
            if (origin == null || destination == null || !graph.nodes.Contains(origin) || !graph.nodes.Contains(destination)) return result;
            var distances = new Dictionary<PedestrianNode, float>();
            var previous = new Dictionary<PedestrianNode, (PedestrianNode node, GraphArc arc)>();
            var settled = new HashSet<PedestrianNode>();
            var order = new Dictionary<PedestrianNode, int>();
            for (int i = 0; i < graph.nodes.Count; i++)
                if (graph.nodes[i] != null) { distances[graph.nodes[i]] = float.PositiveInfinity; order[graph.nodes[i]] = i; }
            var pending = new NodeHeap();
            distances[origin] = 0;
            pending.Push(origin, 0, order[origin]);
            while (pending.Count > 0)
            {
                var entry = pending.Pop();
                var current = entry.node; float best = entry.distance;
                if (best > distances[current] || !settled.Add(current)) continue;
                if (current == destination) break;
                foreach (var arc in graph.Neighbors(current))
                {
                    if (settled.Contains(arc.to)) continue;
                    if (!ignoreClosures && arc.segment.HasClosure) continue;
                    float edgeCost = arc.segment.Length;
                    if (accessible)
                    {
                        var evaluation = AccessibilityEvaluator.Evaluate(arc.segment, profile, arc.forward);
                        if (!evaluation.Passable && !(allowUnknown && evaluation.rating == AccessibilityRating.NotEvaluated)) continue;
                        edgeCost = evaluation.Passable ? evaluation.cost : edgeCost;
                    }
                    float candidate = best + edgeCost;
                    if (candidate >= distances[arc.to]) continue;
                    distances[arc.to] = candidate;
                    previous[arc.to] = (current, arc);
                    pending.Push(arc.to, candidate, order[arc.to]);
                }
            }
            if (float.IsPositiveInfinity(distances[destination])) return result;
            result.nodes.Add(destination);
            var cursor = destination;
            while (cursor != origin)
            {
                var step = previous[cursor];
                result.arcs.Add(step.arc);
                result.nodes.Add(step.node);
                cursor = step.node;
            }
            result.nodes.Reverse(); result.arcs.Reverse();
            result.failure = RouteFailure.None;
            result.cost = distances[destination];
            foreach (var arc in result.arcs)
            {
                var s = arc.segment;
                result.distance += s.Length;
                result.ascent += Mathf.Max(0, (s.end.Position.y - s.start.Position.y) * (arc.forward ? 1 : -1));
                result.maxSlope = Mathf.Max(result.maxSlope, Mathf.Abs(s.SlopePercent));
                result.obstacleCount += s.RelevantObstacles;
                if (AccessibilityEvaluator.Evaluate(s, profile, arc.forward).rating != AccessibilityRating.Accessible) result.problematicCount++;
            }
            result.explanation = accessible
                ? "Menor custo ponderado entre os trechos passáveis e avaliados para o perfil. Impedimentos excluem arestas."
                : "Menor distância na rede aberta. A rota convencional pode conter dificuldades ou barreiras individuais.";
            return result;
        }
        private sealed class NodeHeap
        {
            private readonly List<(PedestrianNode node, float distance, int order)> entries = new List<(PedestrianNode, float, int)>();
            public int Count => entries.Count;
            private static bool Less((PedestrianNode node, float distance, int order) a, (PedestrianNode node, float distance, int order) b) =>
                a.distance < b.distance || (a.distance == b.distance && a.order < b.order);
            public void Push(PedestrianNode node, float distance, int order)
            {
                entries.Add((node, distance, order)); int i = entries.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (!Less(entries[i], entries[parent])) break;
                    (entries[i], entries[parent]) = (entries[parent], entries[i]); i = parent;
                }
            }
            public (PedestrianNode node, float distance, int order) Pop()
            {
                var result = entries[0]; entries[0] = entries[entries.Count-1]; entries.RemoveAt(entries.Count-1);
                int i = 0;
                while (i * 2 + 1 < entries.Count)
                {
                    int child = i * 2 + 1;
                    if (child+1 < entries.Count && Less(entries[child+1], entries[child])) child++;
                    if (!Less(entries[child], entries[i])) break;
                    (entries[i], entries[child]) = (entries[child], entries[i]); i = child;
                }
                return result;
            }
        }
    }
}
