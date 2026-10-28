using UnityEngine;

namespace TrajetoInclusivo
{
    public readonly struct SimulationLayout
    {
        public readonly float scale;
        public readonly Rect left,right,toolbar,footer,map,viewport;
        public SimulationLayout(int width,int height)
        {
            scale=Mathf.Max(.1f,Mathf.Min(width/1440f,height/900f,1.6f));
            float w=width/scale,h=height/scale;
            left=new Rect(16,16,342,h-32);right=new Rect(w-338,16,322,h-32);
            toolbar=new Rect(left.xMax+12,16,right.xMin-left.xMax-24,146);
            footer=new Rect(left.xMax+12,h-65,right.xMin-left.xMax-24,50);
            map=new Rect(toolbar.x,toolbar.yMax+12,toolbar.width,footer.yMin-toolbar.yMax-24);
            // Camera rectangles use a bottom-left origin; IMGUI uses a top-left origin.
            viewport=new Rect(map.x*scale/width,1-map.yMax*scale/height,map.width*scale/width,map.height*scale/height);
        }
    }
}
