using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class RouteVisualizer : MonoBehaviour
    {
        public Material lineMaterial;
        private readonly Dictionary<string, LineRenderer> lines = new Dictionary<string, LineRenderer>();
        private Mesh networkMesh;
        private MeshFilter networkFilter;
        public static Color RatingColor(AccessibilityRating rating)
        {
            switch (rating)
            {
                case AccessibilityRating.Accessible: return new Color(.12f, .82f, .44f);
                case AccessibilityRating.Difficult: return new Color(1, .72f, .08f);
                case AccessibilityRating.Inaccessible: return new Color(.98f, .20f, .22f);
                default: return new Color(.5f, .55f, .6f);
            }
        }
        public void Refresh(SimulationController sim)
        {
            foreach (var line in lines.Values) line.gameObject.SetActive(false);
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            foreach (var s in sim.graph.segments)
            {
                // Network color uses the worse direction; selected route uses actual traversal direction.
                var f = AccessibilityEvaluator.Evaluate(s, sim.Profile, true).rating;
                var r = AccessibilityEvaluator.Evaluate(s, sim.Profile, false).rating;
                var rating = (AccessibilityRating)Mathf.Max((int)f, (int)r);
                var a = s.start.Position + Vector3.up*.08f; var b = s.end.Position + Vector3.up*.08f;
                var side = Vector3.Cross((b-a).normalized,Vector3.up)*.09f;
                int v = vertices.Count;
                vertices.Add(transform.InverseTransformPoint(a-side)); vertices.Add(transform.InverseTransformPoint(a+side));
                vertices.Add(transform.InverseTransformPoint(b+side)); vertices.Add(transform.InverseTransformPoint(b-side));
                triangles.AddRange(new[]{v,v+1,v+2,v,v+2,v+3});
                for(int i=0;i<4;i++)colors.Add(RatingColor(rating));
            }
            if (networkFilter == null)
            {
                var go = new GameObject("Rede combinada por cores");go.transform.SetParent(transform,false);
                networkFilter=go.AddComponent<MeshFilter>();go.AddComponent<MeshRenderer>().sharedMaterial=lineMaterial;
                networkMesh=new Mesh{name="Rede de acessibilidade",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};networkFilter.sharedMesh=networkMesh;
            }
            networkMesh.Clear();networkMesh.SetVertices(vertices);networkMesh.SetTriangles(triangles,0);networkMesh.SetColors(colors);networkMesh.RecalculateNormals();networkMesh.RecalculateBounds();
            ShowRoute(sim.conventional, "conventional", new Color(.2f, .65f, 1), sim.showAccessible ? .13f : .3f, -.45f, sim);
            ShowRoute(sim.accessible, "accessible", Color.white, sim.showAccessible ? .34f : .13f, .28f, sim);
            Endpoint("origin", sim.origin.Position, new Color(.2f, 1, .7f));
            Endpoint("destination", sim.destination.Position, new Color(.9f, .3f, .9f));
        }
        private void Endpoint(string id, Vector3 position, Color color)
        {
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI * 2 / 24, b = (i+1) * Mathf.PI * 2 / 24;
                Draw(id+i, position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.6f,
                    position + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * 1.6f, color, .2f, .2f, 0);
            }
        }
        private void ShowRoute(RouteResult route, string prefix, Color color, float width, float offset, SimulationController sim)
        {
            if (route == null || !route.Found) return;
            for (int i = 0; i < route.arcs.Count; i++)
            {
                var arc = route.arcs[i];
                var rating = AccessibilityEvaluator.Evaluate(arc.segment, sim.Profile, arc.forward).rating;
                Draw(prefix + i, route.nodes[i].Position, route.nodes[i+1].Position,
                    prefix == "accessible" ? RatingColor(rating) : color, width, .14f, offset);
            }
        }
        private void Draw(string id, Vector3 a, Vector3 b, Color color, float width, float height, float offset)
        {
            if (!lines.TryGetValue(id, out var line))
            {
                var go = new GameObject(id); go.transform.SetParent(transform, false);
                line = go.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial;
                line.positionCount = 2; line.useWorldSpace = true; line.numCapVertices = 3;
                lines.Add(id, line);
            }
            line.gameObject.SetActive(true);
            var side = Vector3.Cross(Vector3.up, (b - a).normalized) * offset + Vector3.up * height;
            line.SetPosition(0, a + side); line.SetPosition(1, b + side);
            line.startWidth = line.endWidth = width; line.startColor = line.endColor = color;
        }
        private void OnDestroy(){if(networkMesh!=null)Destroy(networkMesh);}
    }
}
