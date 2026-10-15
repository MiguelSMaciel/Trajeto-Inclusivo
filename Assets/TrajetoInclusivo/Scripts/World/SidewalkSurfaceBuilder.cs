using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrajetoInclusivo
{
    public static class SidewalkSurfaceBuilder
    {
        public static Mesh BuildRaisedPatch(AccessibilityObstacle obstacle,Transform space)
        {
            var r=ObstacleFootprints.Bounds(obstacle);var s=obstacle.segment;var points=new Vector3[8];
            points[0]=ObstacleFootprints.SurfacePoint(s,r.xMin,r.yMin);points[1]=ObstacleFootprints.SurfacePoint(s,r.xMax,r.yMin);
            points[2]=ObstacleFootprints.SurfacePoint(s,r.xMax,r.yMax);points[3]=ObstacleFootprints.SurfacePoint(s,r.xMin,r.yMax);
            for(int i=0;i<4;i++){points[i+4]=space.InverseTransformPoint(points[i]+Vector3.up*obstacle.heightOrDepth);points[i]=space.InverseTransformPoint(points[i]);}
            int[][] faces={new[]{4,5,6,7},new[]{3,2,1,0},new[]{0,1,5,4},new[]{1,2,6,5},new[]{2,3,7,6},new[]{3,0,4,7}};
            var vertices=new Vector3[24];var triangles=new int[36];
            for(int f=0;f<6;f++)
            {
                for(int i=0;i<4;i++)vertices[f*4+i]=points[faces[f][i]];
                int v=f*4,t=f*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            var mesh=new Mesh{name="Degrau ajustado ao plano da calçada",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Build(AccessibilitySegment s,float thickness,Transform space)
        {
            float length=s.HorizontalDistance,half=s.totalWidth*.5f;
            var us=new List<float>{0,length};var vs=new List<float>{-half,half};
            var holes=new List<(Rect rect,float depth)>();
            foreach(var o in s.obstacles)
            {
                if(o==null || !o.isActiveAndEnabled || o.kind!=ObstacleKind.Pothole || o.heightOrDepth<=0)continue;
                var r=ObstacleFootprints.Bounds(o);if(r.width<.001f || r.height<.001f)continue;
                us.Add(r.xMin);us.Add(r.xMax);vs.Add(r.yMin);vs.Add(r.yMax);holes.Add((r,o.heightOrDepth));
            }
            if(s.surfaceAmplitude>0)
            {
                int slices=Mathf.Clamp(Mathf.CeilToInt(length/(Mathf.Max(.2f,s.surfacePeriod)*.25f)),1,256);
                for(int i=1;i<slices;i++)us.Add(length*i/slices);
            }
            SortUnique(us);SortUnique(vs);
            var vertices=new List<Vector3>();var top=new List<int>();var defect=new List<int>();
            float[,] depths=new float[us.Count-1,vs.Count-1];
            for(int i=0;i<us.Count-1;i++)for(int j=0;j<vs.Count-1;j++)
            {
                float u=(us[i]+us[i+1])*.5f,v=(vs[j]+vs[j+1])*.5f,depth=0;
                foreach(var hole in holes)if(hole.rect.Contains(new Vector2(u,v)))depth=Mathf.Max(depth,hole.depth);
                depths[i,j]=depth;
                Quad(P(us[i],vs[j],depth),P(us[i+1],vs[j],depth),P(us[i+1],vs[j+1],depth),P(us[i],vs[j+1],depth),depth>0?defect:top);
            }
            for(int i=0;i<us.Count-1;i++)for(int j=0;j<vs.Count-1;j++)
            {
                float d=depths[i,j];
                if(i+1<us.Count-1 && d!=depths[i+1,j])
                    Quad(P(us[i+1],vs[j],d),P(us[i+1],vs[j],depths[i+1,j]),P(us[i+1],vs[j+1],depths[i+1,j]),P(us[i+1],vs[j+1],d),defect);
                if(j+1<vs.Count-1 && d!=depths[i,j+1])
                    Quad(P(us[i],vs[j+1],d),P(us[i+1],vs[j+1],d),P(us[i+1],vs[j+1],depths[i,j+1]),P(us[i],vs[j+1],depths[i,j+1]),defect);
            }
            float bottom=thickness;
            foreach(var hole in holes)bottom=Mathf.Max(bottom,hole.depth+thickness);
            for(int i=0;i<us.Count-1;i++)
            {
                Quad(P(us[i],-half,depths[i,0]),P(us[i],-half,bottom),P(us[i+1],-half,bottom),P(us[i+1],-half,depths[i,0]),top);
                int j=vs.Count-2;
                Quad(P(us[i+1],half,depths[i,j]),P(us[i+1],half,bottom),P(us[i],half,bottom),P(us[i],half,depths[i,j]),top);
            }
            for(int j=0;j<vs.Count-1;j++)
            {
                Quad(P(0,vs[j+1],depths[0,j]),P(0,vs[j+1],bottom),P(0,vs[j],bottom),P(0,vs[j],depths[0,j]),top);
                int i=us.Count-2;
                Quad(P(length,vs[j],depths[i,j]),P(length,vs[j],bottom),P(length,vs[j+1],bottom),P(length,vs[j+1],depths[i,j]),top);
            }
            Quad(P(0,-half,bottom),P(0,half,bottom),P(length,half,bottom),P(length,-half,bottom),top);
            var mesh=new Mesh{name="Superfície editável "+s.id,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.subMeshCount=2;
            mesh.SetTriangles(top,0);mesh.SetTriangles(defect,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            Vector3 P(float u,float v,float depth)=>ObstacleFootprints.SurfacePoint(s,u,v,depth);
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,List<int> triangles)
            {
                int k=vertices.Count;vertices.Add(space.InverseTransformPoint(a));vertices.Add(space.InverseTransformPoint(b));
                vertices.Add(space.InverseTransformPoint(c));vertices.Add(space.InverseTransformPoint(d));
                triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            }
        }
        private static void SortUnique(List<float> values)
        {
            values.Sort();for(int i=values.Count-1;i>0;i--)if(Mathf.Abs(values[i]-values[i-1])<.00001f)values.RemoveAt(i);
        }
    }
}
