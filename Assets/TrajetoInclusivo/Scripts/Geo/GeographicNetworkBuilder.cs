using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TrajetoInclusivo
{
    public static class GeographicNetworkBuilder
    {
        private sealed class JunctionEnd
        {
            public PedestrianNode node;
            public string featureId;
            public string otherSourceNode;
            public Vector3 offset;
        }
        public static float RoadWidth(ImportedGeoFeature f, GeographicMapDefinition definition)
        {
            if (float.TryParse(f.Property("width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var width) && width > 0) return width;
            if (float.TryParse(f.Property("lanes"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lanes) && lanes > 0) return lanes * definition.assumedLaneWidth;
            return definition.assumedLocalRoadWidth;
        }
        public static void Build(IReadOnlyList<ImportedGeoFeature> features, GeographicMapDefinition definition,
            PedestrianGraph graph, Material nodeMaterial, out PedestrianNode origin, out PedestrianNode destination)
        {
            var junctions = new Dictionary<string, List<JunctionEnd>>();
            var mappedCrossings = new HashSet<string>(features.Where(f => f.Property("highway") == "crossing").Select(f => f.Property("osm_id").Replace("node/", "")));
            var root = new GameObject("Calçadas e conexões SINTÉTICAS sobre eixos OSM").transform;
            root.SetParent(graph.transform, false);
            foreach (var f in features)
            {
                if (f.Property("kind") != "road" || f.parts.Count == 0 || f.parts[0].Length != 2) continue;
                string roadClass = f.Property("highway");
                if (roadClass == "steps" || roadClass == "path" || f.Property("access") == "private") continue;
                if (f.Property("bridge") == "yes" || f.Property("tunnel") == "yes") continue;
                bool footway = roadClass == "footway" || roadClass == "pedestrian";
                if (f.Property("foot") == "no" && string.IsNullOrEmpty(f.Property("sidewalk"))) continue;
                if (f.Property("sidewalk") == "separate" || f.Property("sidewalk") == "no" || f.Property("sidewalk") == "none") continue;
                var refs = f.Property("node_refs").Split(',');
                if (refs.Length != 2) continue;
                var a = f.parts[0][0]; var b = f.parts[0][1];
                if (Vector3.Distance(a, b) < .5f) continue;
                var side = Vector3.Cross((b - a).normalized, Vector3.up);
                foreach (int sign in footway ? new[] { 0 } : new[] { -1, 1 })
                {
                    // side = left of source-way direction. Explicit OSM sidewalk=no/left/right is respected.
                    string sidewalk = f.Property("sidewalk");
                    if (sign == 1 && (sidewalk == "right" || f.Property("sidewalk:left") == "no")) continue;
                    if (sign == -1 && (sidewalk == "left" || f.Property("sidewalk:right") == "no")) continue;
                    float offset = footway ? 0 : RoadWidth(f, definition) * .5f + definition.simulatedSidewalkWidth * .5f + .16f;
                    var shift = side * (offset * sign) + Vector3.up * definition.simulatedSidewalkElevation;
                    var start = Node(f.id + ":" + sign + ":a", a + shift, root, graph);
                    var end = Node(f.id + ":" + sign + ":b", b + shift, root, graph);
                    string name = string.IsNullOrEmpty(f.name) ? "Via OSM sem nome (" + roadClass + ")" : f.name;
                    start.displayName = end.displayName = name;
                    Edge(start, end, f.id + ":" + sign, name, f.Property("osm_id"), false, graph, root, definition);
                    string layer = f.Property("layer"); if (string.IsNullOrEmpty(layer)) layer = "0";
                    AddJunction(junctions, refs[0] + ":" + layer, refs[1], start, f.id, a);
                    AddJunction(junctions, refs[1] + ":" + layer, refs[0], end, f.id, b);
                }
            }
            foreach (var pair in junctions)
            {
                var ends = pair.Value;
                bool allowCrossing = ends.Select(e => e.otherSourceNode).Distinct().Count() > 2 || mappedCrossings.Contains(pair.Key.Split(':')[0]);
                for (int i = 0; i < ends.Count; i++) for (int j = i+1; j < ends.Count; j++)
                {
                    if (ends[i].featureId == ends[j].featureId && ends.Count <= 2) continue; // no arbitrary mid-block crossing
                    if (!allowCrossing && Vector3.Dot(ends[i].offset, ends[j].offset) < 0) continue;
                    float distance = Vector3.Distance(ends[i].node.Position, ends[j].node.Position);
                    if (distance < .12f)
                    {
                        // Merge coincident offset endpoints at the SAME mapped node and layer only.
                        var replaced = ends[j].node; var replacement = ends[i].node;
                        if (replaced == replacement) continue;
                        foreach (var edge in graph.segments)
                        { if (edge.start == replaced) edge.start = replacement; if (edge.end == replaced) edge.end = replacement; }
                        foreach (var entry in ends) if (entry.node == replaced) entry.node = replacement;
                        graph.nodes.Remove(replaced); DestroyGenerated(replaced.gameObject);
                        continue;
                    }
                    // These are explicitly hypothetical junction links, never asserted to be real crossings.
                    if (distance > 35) continue;
                    Edge(ends[i].node, ends[j].node, "junction:" + pair.Key + ":" + i + ":" + j,
                        "Conexão de junção SIMULADA", "node/" + pair.Key.Split(':')[0], true, graph, root, definition);
                }
            }
            RemoveDuplicateEdges(graph);
            var plaza = features.First(f => f.id == definition.plazaId);
            var gate = features.First(f => f.id == definition.campusGateId);
            var plazaPosition = PolygonCentroid(plaza.parts[0]);
            var gatePosition = gate.parts[0][0];
            origin = AddLandmark("A", "Praça Asdrúbal Soares · ponto no polígono OSM", plazaPosition, definition.plazaId, graph, root, definition);
            destination = AddLandmark("B", "Ifes · portão mapeado no OSM (não verificado)", gatePosition, definition.campusGateId, graph, root, definition);
            if(!string.IsNullOrEmpty(definition.faesaReferenceId))
            {
                var reference=features.First(f=>f.id==definition.faesaReferenceId);
                AddLandmark("C","FAESA · Av. Vitória 2220 · referência aproximada na calçada",PolygonCentroid(reference.parts[0]),reference.id,graph,root,definition,true,reference.parts[0]);
            }
            // Sparse, usable selection handles: all junctions and landmark endpoints remain pickable.
            graph.Rebuild();
            foreach (var n in graph.nodes)
            {
                if (n != origin && n != destination && n.id!="C" && graph.Neighbors(n).Count() == 2) continue;
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Selecionar nó";
                sphere.transform.SetParent(n.transform, false); sphere.transform.localPosition = Vector3.up * .65f;
                sphere.transform.localScale = Vector3.one * (n == origin || n == destination || n.id=="C" ? 2.6f : 1.5f);
                sphere.GetComponent<Renderer>().sharedMaterial = nodeMaterial;
            }
        }
        private static void AddJunction(Dictionary<string, List<JunctionEnd>> junctions,
            string key, string otherSourceNode, PedestrianNode node, string featureId, Vector3 p)
        {
            if (!junctions.TryGetValue(key, out var ends)) { ends = new List<JunctionEnd>(); junctions.Add(key, ends); }
            ends.Add(new JunctionEnd { node = node, featureId = featureId, otherSourceNode = otherSourceNode,
                offset = new Vector3(node.Position.x - p.x, 0, node.Position.z - p.z) });
        }
        private static PedestrianNode Node(string id, Vector3 position, Transform root, PedestrianGraph graph)
        {
            var go = new GameObject(id); go.transform.SetParent(root, false); go.transform.position = position;
            var node = go.AddComponent<PedestrianNode>(); node.id = id; graph.nodes.Add(node); return node;
        }
        private static void Edge(PedestrianNode a, PedestrianNode b, string id, string name, string source, bool connection,
            PedestrianGraph graph, Transform root, GeographicMapDefinition definition)
        {
            if (a == b) return;
            var go = new GameObject(id.Replace('/', '_')); go.transform.SetParent(root, false);
            var s = go.AddComponent<AccessibilitySegment>(); s.id = id; s.displayName = name; s.start = a; s.end = b;
            s.totalWidth = s.freeWidth = definition.simulatedSidewalkWidth; s.crossSlopePercent = 0; s.roughness = 0;
            s.hasAssessmentData = false; s.verification = VerificationState.Simulated; s.sourceFeatureId = source;
            s.syntheticConnection = connection; s.elevationIsSimulated = true;
            s.geometryProvenance = connection ? "Conexão artificial; não confirma a existência de travessia ou acesso no local."
                : "Calçada aproximada por deslocamento do eixo OSM. Largura, cota e avaliação não levantadas.";
            s.pavement = "Piso não levantado (hipótese plana no modo de teste)";
            graph.segments.Add(s);
        }
        private static PedestrianNode AddLandmark(string id, string name, Vector3 mappedPosition, string source,
            PedestrianGraph graph, Transform root, GeographicMapDefinition definition,bool projectedFrontage=false,Vector3[] frontageFootprint=null)
        {
            // Snap to a segment by projection, then split that edge; do not snap to a distant endpoint.
            AccessibilitySegment nearest = null; Vector3 snapped = default; float best = float.PositiveInfinity;
            var candidates=projectedFrontage?frontageFootprint:new[]{mappedPosition};
            foreach (var s in graph.segments)
            {
                if (s.syntheticConnection) continue;
                if(projectedFrontage && s.displayName!="Avenida Vitória")continue;
                var delta = s.end.Position - s.start.Position;
                foreach(var candidate in candidates)
                {
                    float t = Mathf.Clamp01(Vector3.Dot(candidate - s.start.Position, delta) / delta.sqrMagnitude);
                    var p = s.start.Position + delta * t; float distance = (p - candidate).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = s; snapped = p; }
                }
            }
            if (nearest == null || best > 10000) throw new InvalidOperationException("Referência sem calçada aproximada próxima: " + name);
            PedestrianNode joined;
            if (Vector3.Distance(snapped, nearest.start.Position) < .2f) joined = nearest.start;
            else if (Vector3.Distance(snapped, nearest.end.Position) < .2f) joined = nearest.end;
            else
            {
                joined = Node(id + ":snap", snapped, root, graph); joined.displayName = name + " · projeção aproximada";
                var oldEnd = nearest.end; nearest.end = joined;
                Edge(joined, oldEnd, nearest.id + ":split", nearest.displayName, nearest.sourceFeatureId, false, graph, root, definition);
            }
            if(projectedFrontage)
            {
                // Reuse the projected network node: a zero-length artificial access would be impassable.
                joined.id=id;joined.displayName=name;return joined;
            }
            var landmark = Node(id, mappedPosition + Vector3.up * definition.simulatedSidewalkElevation, root, graph); landmark.displayName = name;
            Edge(landmark, joined, "access:" + id, "Acesso local SIMULADO · " + name, source, true, graph, root, definition);
            return landmark;
        }
        public static Vector3 PolygonCentroid(Vector3[] polygon)
        {
            double area = 0, x = 0, z = 0;
            for (int i = 0; i < polygon.Length - 1; i++)
            {
                double cross = (double)polygon[i].x * polygon[i+1].z - (double)polygon[i+1].x * polygon[i].z;
                area += cross; x += (polygon[i].x + polygon[i+1].x) * cross; z += (polygon[i].z + polygon[i+1].z) * cross;
            }
            if (Math.Abs(area) < .001) return polygon[0];
            return new Vector3((float)(x / (3 * area)), 0, (float)(z / (3 * area)));
        }
        private static void RemoveDuplicateEdges(PedestrianGraph graph)
        {
            var seen = new HashSet<(PedestrianNode, PedestrianNode)>();
            foreach (var s in graph.segments.ToArray())
            {
                if (s.start == s.end || seen.Contains((s.start, s.end)) || seen.Contains((s.end, s.start)))
                { graph.segments.Remove(s); DestroyGenerated(s.gameObject); }
                else seen.Add((s.start, s.end));
            }
        }
        private static void DestroyGenerated(GameObject go)
        { if (Application.isPlaying) UnityEngine.Object.Destroy(go); else UnityEngine.Object.DestroyImmediate(go); }
    }
}
