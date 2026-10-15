using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed partial class SimulationUI
    {
        public readonly struct LandmarkMarker
        {
            public readonly PedestrianNode node;
            public readonly Rect rect;
            public readonly string label;
            public readonly bool outsideView;
            public readonly Vector2 anchor;
            public LandmarkMarker(PedestrianNode node,Rect rect,string label,bool outsideView,Vector2 anchor)
            {this.node=node;this.rect=rect;this.label=label;this.outsideView=outsideView;this.anchor=anchor;}
        }
        private PedestrianNode[] landmarkNodes;
        private bool frameNextRoute;
        private GUIStyle landmarkButton;
        private readonly List<(Renderer renderer,bool enabled)> groundLabels=new List<(Renderer,bool)>();
        private bool? worldLabelsVisible;
        private void Start()
        {
            simulation.RoutesChanged+=HandleNavigationRouteChanged;
            if(simulation.geographicContext!=null)
                foreach(var text in simulation.GetComponentsInChildren<TextMesh>())
                    if(text.GetComponentInParent<PedestrianNode>()==null){var renderer=text.GetComponent<Renderer>();groundLabels.Add((renderer,renderer.enabled));}
        }
        private void LateUpdate()
        {
            bool show=cameraController.distance>=130;if(worldLabelsVisible==show)return;worldLabelsVisible=show;
            foreach(var label in groundLabels)if(label.renderer!=null)label.renderer.enabled=show && label.enabled;
        }
        private void OnDestroy()
        {if(simulation!=null)simulation.RoutesChanged-=HandleNavigationRouteChanged;}
        private void HandleNavigationRouteChanged()
        {
            if(!frameNextRoute)return;frameNextRoute=false;
            var route=simulation.showAccessible && simulation.accessible.Found?simulation.accessible:simulation.conventional;
            cameraController.FrameRoute(route);
        }
        private PedestrianNode[] LandmarkNodes()
        {
            if(landmarkNodes!=null)return landmarkNodes;
            if(simulation.geographicContext==null)return landmarkNodes=new PedestrianNode[0];
            var nodes=new List<PedestrianNode>();
            foreach(string id in new[]{"A","B","C"})
            {var node=simulation.graph.nodes.Find(n=>n.id==id);if(node!=null)nodes.Add(node);}
            return landmarkNodes=nodes.ToArray();
        }
        private static string LandmarkName(PedestrianNode node)
        {return node.id=="A"?"Praça":node.id=="B"?"Ifes":"FAESA (ref.)";}
        public void FocusLandmark(PedestrianNode node)
        {
            if(node==null || simulation.geographicContext==null || !simulation.graph.nodes.Contains(node))return;
            CancelPicking();inspection.Stop();frameNextRoute=false;
            cameraController.FramePoint(node.Position);
        }
        private void DrawLandmarkNavigation()
        {
            var nodes=LandmarkNodes();if(nodes.Length==0)return;
            GUILayout.BeginHorizontal();GUILayout.Label("Aproximar:",small,GUILayout.Width(76));
            foreach(var node in nodes)if(GUILayout.Button(LandmarkName(node),button))FocusLandmark(node);
            GUILayout.EndHorizontal();
        }
        public LandmarkMarker[] GetLandmarkMarkers()
        {
            var visible=new List<PedestrianNode>();var anchors=new List<Vector2>();var outside=new List<bool>();
            var view=cameraController.view;
            foreach(var node in LandmarkNodes())
            {
                var p=view.WorldToViewportPoint(node.Position+Vector3.up*1.5f);if(p.z<view.nearClipPlane)continue;
                visible.Add(node);anchors.Add(new Vector2((SceneViewport.x+p.x*SceneViewport.width)*VirtualWidth,(1-p.y)*VirtualHeight));
                outside.Add(p.x<0 || p.x>1 || p.y<0 || p.y>1);
            }
            var area=new Rect(SceneViewport.x*VirtualWidth+8,Toolbar.yMax+8,SceneViewport.width*VirtualWidth-16,Layout.footer.yMin-Toolbar.yMax-16);
            var rectangles=LandmarkLabelLayout.Arrange(area,anchors);var result=new LandmarkMarker[visible.Count];
            for(int i=0;i<result.Length;i++)
            {
                var node=visible[i];string role=node==simulation.origin && node==simulation.destination?" · A/B":node==simulation.origin?" · A":node==simulation.destination?" · B":"";
                var anchor=new Vector2(Mathf.Clamp(anchors[i].x,area.xMin,area.xMax),Mathf.Clamp(anchors[i].y,area.yMin,area.yMax));
                result[i]=new LandmarkMarker(node,rectangles[i],LandmarkName(node)+role+(outside[i]?" · fora da vista":""),outside[i],anchor);
            }
            return result;
        }
        private void DrawLandmarkMarkers()
        {
            if(landmarkButton==null)
            {
                landmarkButton=new GUIStyle(button){fontSize=12,padding=new RectOffset(6,6,4,4)};
                foreach(var state in new[]{landmarkButton.normal,landmarkButton.hover,landmarkButton.active,landmarkButton.focused}){state.background=null;state.textColor=Color.white;}
                landmarkButton.hover.textColor=new Color(.4f,1,.85f);
            }
            foreach(var marker in GetLandmarkMarkers())
            {
                var delta=marker.rect.center-marker.anchor;var matrix=GUI.matrix;
                GUI.color=new Color(.35f,.95f,.86f);
                GUI.matrix=matrix*Matrix4x4.TRS(new Vector3(marker.anchor.x,marker.anchor.y,0),Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg),Vector3.one);
                GUI.DrawTexture(new Rect(0,0,delta.magnitude,1.5f),Texture2D.whiteTexture);GUI.matrix=matrix;GUI.color=Color.white;
                DrawPanel(marker.rect);
                if(GUI.Button(marker.rect,new GUIContent(marker.label,marker.node.displayName+" · Clique para aproximar."),landmarkButton))FocusLandmark(marker.node);
            }
        }
    }
}
