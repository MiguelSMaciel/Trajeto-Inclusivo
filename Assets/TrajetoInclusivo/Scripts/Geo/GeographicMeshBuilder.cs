using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrajetoInclusivo
{
    public sealed class GeographicMeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        public int VertexCount => vertices.Count;
        public void Road(Vector3 a, Vector3 b, float width, float elevation = 0)
        {
            var side = Vector3.Cross((b-a).normalized, Vector3.up) * width * .5f;
            a.y = b.y = elevation;
            Quad(a-side, a+side, b+side, b-side);
        }
        public bool Polygon(Vector3[] input, float baseHeight, float height)
        {
            var points = new List<Vector3>();
            foreach (var p in input)
                if (points.Count == 0 || (p - points[points.Count-1]).sqrMagnitude > .0001f) points.Add(new Vector3(p.x, baseHeight + height, p.z));
            if (points.Count > 1 && (points[0]-points[points.Count-1]).sqrMagnitude < .0001f) points.RemoveAt(points.Count-1);
            if (points.Count < 3) return false;
            float area = 0;
            for (int i = 0; i < points.Count; i++) { var a = points[i]; var b = points[(i+1)%points.Count]; area += a.x*b.z - b.x*a.z; }
            // Clockwise in XZ gives upward-facing Unity triangles.
            if (area > 0) points.Reverse();
            var remaining = new List<int>(); for (int i = 0; i < points.Count; i++) remaining.Add(i);
            var roof = new List<int>();
            int attempts = points.Count * points.Count;
            while (remaining.Count > 3 && attempts-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i+remaining.Count-1)%remaining.Count], b = remaining[i], c = remaining[(i+1)%remaining.Count];
                    if (Cross(points[a], points[b], points[c]) >= -.000001f) continue;
                    bool contains = false;
                    foreach (int j in remaining)
                        if (j != a && j != b && j != c && Inside(points[j], points[a], points[b], points[c])) { contains = true; break; }
                    if (contains) continue;
                    roof.Add(a); roof.Add(b); roof.Add(c); remaining.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) return false; // reject invalid footprints instead of silently drawing a wrong fan
            }
            if (remaining.Count != 3) return false;
            roof.AddRange(remaining);
            int offset = vertices.Count; vertices.AddRange(points);
            foreach (int index in roof) triangles.Add(offset + index);
            if (height > 0)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var a = points[i]; var b = points[(i+1)%points.Count];
                    Quad(a, a - Vector3.up*height, b - Vector3.up*height, b);
                }
            }
            return true;
        }
        public void Outline(Vector3[] points, float width, float elevation)
        { for (int i = 0; i < points.Length-1; i++) Road(points[i], points[i+1], width, elevation); }
        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int v = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.AddRange(new[] {v,v+1,v+2,v,v+2,v+3});
        }
        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private static float Cross(Vector3 a, Vector3 b, Vector3 c) => (b.x-a.x)*(c.z-a.z) - (b.z-a.z)*(c.x-a.x);
        private static bool Inside(Vector3 p, Vector3 a, Vector3 b, Vector3 c) => Cross(a,b,p) <= 0 && Cross(b,c,p) <= 0 && Cross(c,a,p) <= 0;
    }
}
