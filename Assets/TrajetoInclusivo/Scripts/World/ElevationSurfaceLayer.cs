using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrajetoInclusivo
{
    // The CSV describes circulation-surface Y. The reference ground stays 0.4 m below it,
    // leaving room for independent sidewalk defects. This is not a surveyed terrain surface.
    public sealed class ElevationSurfaceLayer:MonoBehaviour
    {
        private sealed class Source
        {public MeshFilter filter;public Mesh original,dynamic;public MeshCollider collider;public Collider primitive;}
        private readonly List<Source> sources=new List<Source>();
        private readonly List<(TextMesh text,Vector3 position)> labels=new List<(TextMesh,Vector3)>();
        private GameObject originalGround,terrain;
        private Mesh terrainMesh;
        private Bounds bounds;
        private Material material;
        public void Initialize(SimulationController sim)
        {
            var inspection=sim.GetComponent<RouteInspection>();var marker=inspection==null?null:inspection.marker;
            foreach(var filter in sim.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.GetComponentInParent<AccessibilitySegment>()!=null || filter.GetComponentInParent<PedestrianNode>()!=null || (marker!=null && filter.transform.IsChildOf(marker)))continue;
                if(filter.name=="Base sintética" || filter.name.StartsWith("Plano de referência artificial"))
                {originalGround=filter.gameObject;bounds=filter.GetComponent<Renderer>().bounds;material=filter.GetComponent<Renderer>().sharedMaterial;continue;}
                sources.Add(new Source{filter=filter,original=filter.sharedMesh,collider=filter.GetComponent<MeshCollider>(),primitive=filter.GetComponent<Collider>()});
            }
            foreach(var text in sim.GetComponentsInChildren<TextMesh>())if(text.GetComponentInParent<PedestrianNode>()==null)labels.Add((text,text.transform.position));
        }
        public void Apply(ElevationDataset data)
        {
            foreach(var source in sources)
            {
                if(source.filter==null)continue;
                if(source.dynamic!=null){Destroy(source.dynamic);source.dynamic=null;}
                if(data==null)
                {
                    source.filter.sharedMesh=source.original;
                    if(source.collider!=null){source.collider.sharedMesh=source.original;source.collider.enabled=source.primitive==source.collider;}
                    if(source.primitive!=null)source.primitive.enabled=true;continue;
                }
                var mesh=Instantiate(source.original);mesh.name="Relevo de referência — "+source.original.name;
                var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++)
                {
                    var world=source.filter.transform.TransformPoint(vertices[i]);
                    if(data.TrySample(world.x,world.z,out var y)){world.y+=y-.18f;vertices[i]=source.filter.transform.InverseTransformPoint(world);}
                }
                mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();source.dynamic=mesh;source.filter.sharedMesh=mesh;
                if(source.primitive!=null && !(source.primitive is MeshCollider))source.primitive.enabled=false;
                if(source.collider==null)source.collider=source.filter.gameObject.AddComponent<MeshCollider>();
                source.collider.enabled=true;source.collider.sharedMesh=mesh;
            }
            foreach(var label in labels)
            {
                if(label.text==null)continue;var p=label.position;
                if(data!=null && data.TrySample(p.x,p.z,out var y))p.y+=y-.18f;label.text.transform.position=p;
            }
            if(originalGround==null)return;
            originalGround.SetActive(data==null);
            if(terrain!=null){terrain.SetActive(false);Destroy(terrain);terrain=null;}
            if(terrainMesh!=null){Destroy(terrainMesh);terrainMesh=null;}
            if(data==null)return;
            terrain=new GameObject("Superfície de referência derivada da grade");terrain.transform.SetParent(transform,false);
            const int n=80;var points=new Vector3[(n+1)*(n+1)];var triangles=new int[n*n*6];
            for(int j=0;j<=n;j++)for(int i=0;i<=n;i++)
            {
                float x=Mathf.Lerp(bounds.min.x,bounds.max.x,i/(float)n),z=Mathf.Lerp(bounds.min.z,bounds.max.z,j/(float)n);
                float y=data.TrySample(x,z,out var h)?h-.4f:bounds.max.y;points[j*(n+1)+i]=transform.InverseTransformPoint(new Vector3(x,y,z));
            }
            for(int j=0;j<n;j++)for(int i=0;i<n;i++)
            {
                int v=j*(n+1)+i,t=(j*n+i)*6;triangles[t]=v;triangles[t+1]=v+n+1;triangles[t+2]=v+1;
                triangles[t+3]=v+1;triangles[t+4]=v+n+1;triangles[t+5]=v+n+2;
            }
            terrainMesh=new Mesh{name="Grade de referência",indexFormat=IndexFormat.UInt32,vertices=points,triangles=triangles};terrainMesh.RecalculateNormals();terrainMesh.RecalculateBounds();
            terrain.AddComponent<MeshFilter>().sharedMesh=terrainMesh;terrain.AddComponent<MeshRenderer>().sharedMaterial=material;terrain.AddComponent<MeshCollider>().sharedMesh=terrainMesh;
        }
        private void OnDestroy(){foreach(var s in sources)if(s.dynamic!=null)Destroy(s.dynamic);if(terrainMesh!=null)Destroy(terrainMesh);}
    }
}
