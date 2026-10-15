using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public static class ObstacleFootprints
    {
        // x = distance along the horizontal segment; y = lateral distance (right of its direction).
        public static Rect Bounds(AccessibilityObstacle obstacle)
        {
            var s = obstacle.segment;
            float length = Mathf.Max(.001f, s.HorizontalDistance), half = s.totalWidth*.5f;
            float width = obstacle.useFootprintForClearance ? obstacle.occupiedWidth : Mathf.Max(0,s.totalWidth-obstacle.remainingFreeWidth);
            if (obstacle.kind == ObstacleKind.Step && !obstacle.useFootprintForClearance) width=s.totalWidth;
            if (obstacle.blocksPassage && !obstacle.canBypass) width=s.totalWidth;
            width=Mathf.Clamp(width,0,s.totalWidth);
            float center = obstacle.useFootprintForClearance ? Mathf.Clamp(obstacle.lateralOffset,-half+width*.5f,half-width*.5f) : half-width*.5f;
            float u=Mathf.Clamp01(obstacle.positionAlongSegment)*length;
            return Rect.MinMaxRect(Mathf.Max(0,u-obstacle.length*.5f),center-width*.5f,
                Mathf.Min(length,u+obstacle.length*.5f),center+width*.5f);
        }
        public static float FreeWidth(AccessibilitySegment s, float blockingStepThreshold=float.PositiveInfinity)
        {
            if(s.obstacles.Count==0)return Mathf.Clamp(s.freeWidth,0,s.totalWidth);
            var relevant=new List<AccessibilityObstacle>();var cuts=new List<float>{0,s.HorizontalDistance};
            foreach(var o in s.obstacles)
            {
                if(o==null || !o.isActiveAndEnabled)continue;
                if(o.kind==ObstacleKind.Step && o.heightOrDepth<=blockingStepThreshold)continue;
                var rect=Bounds(o);if(rect.width<=.0001f || rect.height<=.0001f)continue;
                relevant.Add(o);cuts.Add(rect.xMin);cuts.Add(rect.xMax);
            }
            cuts.Sort();float free=Mathf.Clamp(s.freeWidth,0,s.totalWidth);
            for(int i=0;i<cuts.Count-1;i++)
            {
                if(cuts[i+1]-cuts[i]<.00001f)continue;
                float u=(cuts[i]+cuts[i+1])*.5f;
                var intervals=new List<Vector2>();
                foreach(var o in relevant){var r=Bounds(o);if(u>=r.xMin && u<=r.xMax)intervals.Add(new Vector2(r.yMin,r.yMax));}
                intervals.Sort((a,b)=>a.x.CompareTo(b.x));
                float cursor=-s.totalWidth*.5f,gap=0;
                foreach(var interval in intervals){gap=Mathf.Max(gap,interval.x-cursor);cursor=Mathf.Max(cursor,interval.y);}
                gap=Mathf.Max(gap,s.totalWidth*.5f-cursor);free=Mathf.Min(free,gap);
            }
            return Mathf.Max(0,free);
        }
        public static bool StepBlocks(AccessibilitySegment s, AccessibilityProfile p)
        {
            foreach(var o in s.obstacles)
                if(o!=null && o.isActiveAndEnabled && o.kind==ObstacleKind.Step && !o.canBypass && o.heightOrDepth>p.maxStepHeight)return true;
            return FreeWidth(s,p.maxStepHeight)<p.requiredWidth;
        }
        public static Vector2 LocalPoint(AccessibilitySegment s, Vector3 world)
        {
            var direction=new Vector3(s.end.Position.x-s.start.Position.x,0,s.end.Position.z-s.start.Position.z).normalized;
            var side=Vector3.Cross(Vector3.up,direction);var delta=world-s.start.Position;
            return new Vector2(Vector3.Dot(delta,direction),Vector3.Dot(delta,side));
        }
        public static Vector3 SurfacePoint(AccessibilitySegment s,float u,float v,float depression=0)
        {
            float length=Mathf.Max(.001f,s.HorizontalDistance),t=Mathf.Clamp01(u/length);
            var direction=new Vector3(s.end.Position.x-s.start.Position.x,0,s.end.Position.z-s.start.Position.z).normalized;
            float wave=s.surfaceAmplitude*Mathf.Sin(2*Mathf.PI*u/Mathf.Max(.2f,s.surfacePeriod))*Mathf.Sin(Mathf.PI*t);
            return Vector3.Lerp(s.start.Position,s.end.Position,t)+Vector3.Cross(Vector3.up,direction)*v
                +Vector3.up*(v*s.crossSlopePercent/100f+wave-depression);
        }
        public static int GeometryHash(AccessibilitySegment s)
        {
            unchecked
            {
                int hash=s.surfaceAmplitude.GetHashCode()*31+s.surfacePeriod.GetHashCode();
                foreach(var o in s.obstacles)
                {
                    if(o==null || !o.isActiveAndEnabled || o.kind!=ObstacleKind.Pothole)continue;
                    var b=Bounds(o);hash=hash*31+b.GetHashCode();hash=hash*31+o.heightOrDepth.GetHashCode();
                }
                return hash;
            }
        }
    }
}
