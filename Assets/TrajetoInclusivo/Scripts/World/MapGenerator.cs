using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class MapGenerator : MonoBehaviour
    {
        public PedestrianGraph graph;
        public Material sidewalkMaterial, curbMaterial, obstacleMaterial, potholeMaterial;
        [Min(.01f)] public float slabThickness = .18f;
        public bool generateConnectionSurfaces = true;
        private readonly HashSet<Mesh> ownedMeshes = new HashSet<Mesh>();
        private readonly Dictionary<AccessibilitySegment, (Vector3 a, Vector3 b, float width, float cross, float thickness,int detail)> geometryCache =
            new Dictionary<AccessibilitySegment, (Vector3, Vector3, float, float, float,int)>();

        public void RefreshGeometry()
        {
            foreach (var s in graph.segments)
            {
                if (s.syntheticConnection && !generateConnectionSurfaces)
                {
                    // Hypothetical connections are guide lines with selection hitboxes, not claimed curb geometry.
                    var box = s.GetComponent<BoxCollider>(); if (box == null) box = s.gameObject.AddComponent<BoxCollider>();
                    var direction = s.end.Position-s.start.Position;
                    s.transform.SetPositionAndRotation((s.start.Position+s.end.Position)*.5f, Quaternion.LookRotation(direction.normalized));
                    box.center = Vector3.zero; box.size = new Vector3(s.totalWidth,.12f,direction.magnitude);
                    foreach (var o in s.obstacles) if (o != null && o.isActiveAndEnabled) RefreshObstacle(o);
                    continue;
                }
                var state = (s.start.Position, s.end.Position, s.totalWidth, s.crossSlopePercent, slabThickness,ObstacleFootprints.GeometryHash(s));
                if (!geometryCache.TryGetValue(s, out var oldState) || !state.Equals(oldState))
                {
                    UpdateSlab(s, "Pavimento", 0, s.totalWidth, sidewalkMaterial, 0);
                    UpdateSlab(s, "Meio-fio esquerdo", s.totalWidth * .5f + .08f, .16f, curbMaterial, -.03f);
                    UpdateSlab(s, "Meio-fio direito", -s.totalWidth * .5f - .08f, .16f, curbMaterial, -.03f);
                    geometryCache[s] = state;
                }
                foreach (var o in s.obstacles) if (o != null && o.isActiveAndEnabled) RefreshObstacle(o);
            }
        }
        private void UpdateSlab(AccessibilitySegment s, string childName, float lateral, float width, Material material, float y)
        {
            var child = s.transform.Find(childName);
            if (child == null)
            {
                child = new GameObject(childName).transform; child.SetParent(s.transform, false);
                child.gameObject.AddComponent<MeshFilter>(); child.gameObject.AddComponent<MeshRenderer>();
                child.gameObject.AddComponent<MeshCollider>();
            }
            var a = s.start.Position; var b = s.end.Position;
            var direction = new Vector3(b.x - a.x, 0, b.z - a.z).normalized;
            var side = Vector3.Cross(Vector3.up, direction);
            a += side * lateral + Vector3.up * y; b += side * lateral + Vector3.up * y;
            // Crossfall is represented by the actual slab surface, not only an annotation.
            var half = side * width * .5f + Vector3.up * (width * .5f * s.crossSlopePercent / 100f);
            var top = new[] { a - half, a + half, b + half, b - half };
            var vertices = new Vector3[24]; var triangles = new int[36];
            int[][] faces = { new[] {0,3,2,1}, new[] {4,5,6,7}, new[] {0,1,5,4}, new[] {1,2,6,5}, new[] {2,3,7,6}, new[] {3,0,4,7} };
            var corners = new Vector3[8];
            for (int i = 0; i < 4; i++) { corners[i] = top[i]; corners[i + 4] = top[i] - Vector3.up * slabThickness; }
            for (int f = 0; f < 6; f++)
            {
                for (int j = 0; j < 4; j++) vertices[f * 4 + j] = child.InverseTransformPoint(corners[faces[f][j]]);
                int v = f * 4, t = f * 6;
                triangles[t] = v; triangles[t+1] = v+1; triangles[t+2] = v+2;
                triangles[t+3] = v; triangles[t+4] = v+2; triangles[t+5] = v+3;
            }
            var filter = child.GetComponent<MeshFilter>();
            var old = filter.sharedMesh;
            var mesh = new Mesh { name = "Trecho " + s.id + " " + childName, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            bool detailed=childName=="Pavimento" && (s.surfaceAmplitude>0 || s.obstacles.Exists(o=>o!=null && o.isActiveAndEnabled && o.kind==ObstacleKind.Pothole));
            if(detailed)
            {
                if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);
                mesh=SidewalkSurfaceBuilder.Build(s,slabThickness,child);
            }
            filter.sharedMesh = mesh; child.GetComponent<MeshCollider>().sharedMesh = mesh;
            child.GetComponent<MeshRenderer>().sharedMaterials = detailed?new[]{material,potholeMaterial}:new[]{material};
            if (Application.isPlaying)
            {
                ownedMeshes.Add(mesh);
                if (old != null && ownedMeshes.Remove(old)) Destroy(old);
            }
        }
        public void RefreshObstacle(AccessibilityObstacle o)
        {
            var s = o.segment;
            var a = s.start.Position; var b = s.end.Position;
            var direction = new Vector3(b.x - a.x, 0, b.z - a.z).normalized;
            var side = Vector3.Cross(Vector3.up, direction);
            var footprint=ObstacleFootprints.Bounds(o);
            // The pothole is represented by the actual pavement depression. Picking resolves its footprint.
            if(o.kind==ObstacleKind.Pothole)
            {
                if(o.visual!=null)o.visual.gameObject.SetActive(false);
                o.transform.position=ObstacleFootprints.SurfacePoint(s,footprint.center.x,footprint.center.y);
                o.transform.rotation=Quaternion.LookRotation(direction);return;
            }
            if(o.visual!=null && o.visual.name!="Volume "+o.kind)
            {o.visual.gameObject.SetActive(false);if(Application.isPlaying)Destroy(o.visual.gameObject);else DestroyImmediate(o.visual.gameObject);o.visual=null;}
            if(o.kind==ObstacleKind.Step)
            {
                if(o.visual==null)
                {
                    o.visual=new GameObject("Volume "+o.kind).transform;o.visual.SetParent(o.transform,false);
                    o.visual.gameObject.AddComponent<MeshFilter>();o.visual.gameObject.AddComponent<MeshRenderer>();o.visual.gameObject.AddComponent<MeshCollider>();
                }
                o.transform.SetPositionAndRotation(ObstacleFootprints.SurfacePoint(s,footprint.center.x,footprint.center.y),Quaternion.LookRotation(direction));
                o.visual.localPosition=Vector3.zero;o.visual.localRotation=Quaternion.identity;o.visual.localScale=Vector3.one;o.visual.gameObject.SetActive(true);
                int hash=System.HashCode.Combine(s.start.Position,s.end.Position,s.crossSlopePercent,s.surfaceAmplitude,s.surfacePeriod,footprint,o.heightOrDepth);
                if(o.generatedGeometry==null || o.generatedGeometryHash!=hash)
                {
                    var old=o.generatedGeometry;o.generatedGeometry=SidewalkSurfaceBuilder.BuildRaisedPatch(o,o.visual);o.generatedGeometryHash=hash;
                    o.visual.GetComponent<MeshFilter>().sharedMesh=o.generatedGeometry;o.visual.GetComponent<MeshCollider>().sharedMesh=o.generatedGeometry;
                    if(old!=null){if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);}
                }
                o.visual.GetComponent<MeshRenderer>().sharedMaterial=obstacleMaterial;return;
            }
            if (o.visual == null)
            {
                o.visual=new GameObject("Volume "+o.kind).transform;o.visual.SetParent(o.transform,false);
                switch(o.kind)
                {
                    case ObstacleKind.Pole: Part(PrimitiveType.Cylinder,"Poste",Vector3.zero,new Vector3(1,.5f,1));break;
                    case ObstacleKind.Tree:
                        Part(PrimitiveType.Cylinder,"Tronco",new Vector3(0,-.175f,0),new Vector3(.32f,.325f,.32f));
                        Part(PrimitiveType.Sphere,"Copa",new Vector3(0,.15f,0),new Vector3(1,.7f,1));break;
                    case ObstacleKind.Furniture:
                        Part(PrimitiveType.Cube,"Banco",new Vector3(0,-.2f,0),new Vector3(1,.2f,1));
                        Part(PrimitiveType.Cube,"Encosto",new Vector3(0,.15f,.4f),new Vector3(1,.7f,.15f));
                        Part(PrimitiveType.Cube,"Pé esquerdo",new Vector3(-.4f,-.375f,0),new Vector3(.12f,.25f,.8f));
                        Part(PrimitiveType.Cube,"Pé direito",new Vector3(.4f,-.375f,0),new Vector3(.12f,.25f,.8f));break;
                    default:Part(PrimitiveType.Cube,"Volume físico",Vector3.zero,Vector3.one);break;
                }
            }
            o.visual.gameObject.SetActive(true);
            float width=Mathf.Max(.02f,footprint.height),height=Mathf.Max(.01f,o.heightOrDepth);
            o.transform.position=ObstacleFootprints.SurfacePoint(s,footprint.center.x,footprint.center.y);
            o.transform.rotation = Quaternion.LookRotation(direction);
            o.visual.position=o.transform.position+Vector3.up*(height*.5f);
            o.visual.rotation = o.transform.rotation;
            o.visual.localScale=new Vector3(width,height,Mathf.Max(.02f,footprint.width));
            void Part(PrimitiveType type,string name,Vector3 position,Vector3 scale)
            {
                var part=GameObject.CreatePrimitive(type);part.name=name;part.transform.SetParent(o.visual,false);
                part.transform.localPosition=position;part.transform.localScale=scale;
                part.GetComponent<Renderer>().sharedMaterial=obstacleMaterial;
            }
        }
        private void OnDestroy()
        {
            foreach (var mesh in ownedMeshes) if (mesh != null) Destroy(mesh);
            ownedMeshes.Clear();
        }
    }
}
