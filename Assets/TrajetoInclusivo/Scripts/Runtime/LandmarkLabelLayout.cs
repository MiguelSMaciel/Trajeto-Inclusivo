using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public static class LandmarkLabelLayout
    {
        // Screen-space labels may move vertically to remain readable; they never move graph nodes.
        public static Rect[] Arrange(Rect area,IReadOnlyList<Vector2> anchors)
        {
            var result=new Rect[anchors.Count];float width=Mathf.Min(184,area.width),height=38;
            for(int i=0;i<anchors.Count;i++)
            {
                var rect=new Rect(Mathf.Clamp(anchors[i].x-width*.5f,area.xMin,area.xMax-width),
                    Mathf.Clamp(anchors[i].y-height-10,area.yMin,area.yMax-height),width,height);
                float desired=rect.y;bool found=false;
                for(int offset=0;offset<=anchors.Count && !found;offset++)for(int sign=-1;sign<=1 && !found;sign+=2)
                {
                    rect.y=Mathf.Clamp(desired+sign*offset*(height+6),area.yMin,area.yMax-height);bool collision=false;
                    for(int j=0;j<i;j++)
                    {var padded=result[j];padded.xMin-=3;padded.xMax+=3;padded.yMin-=3;padded.yMax+=3;if(rect.Overlaps(padded)){collision=true;break;}}
                    if(!collision)found=true;
                }
                result[i]=rect;
            }
            return result;
        }
    }
}
