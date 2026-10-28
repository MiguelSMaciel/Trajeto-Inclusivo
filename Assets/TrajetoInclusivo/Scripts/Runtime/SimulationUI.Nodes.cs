using System.Linq;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed partial class SimulationUI
    {
        public PedestrianNode SelectedNode{get;private set;}
        private void EnsureScenarioNodeHandles()
        {
            var template=simulation.graph.nodes.Select(n=>n.GetComponentInChildren<SphereCollider>()).FirstOrDefault(c=>c!=null);
            if(template==null)return;
            foreach(string id in simulation.scenarios.scenarios.Where(s=>s.elevationOffset!=0).Select(s=>s.raisedNodeId).Distinct())
            {
                var node=simulation.graph.nodes.Find(n=>n.id==id);
                if(node==null || node.GetComponentInChildren<SphereCollider>()!=null)continue;
                var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="Selecionar nó do cenário";
                sphere.transform.SetParent(node.transform,false);sphere.transform.localPosition=Vector3.up*.65f;sphere.transform.localScale=Vector3.one*1.5f;
                sphere.GetComponent<Renderer>().sharedMaterial=template.GetComponent<Renderer>().sharedMaterial;
            }
        }
        private void SetInspectedNode(PedestrianNode node)
        {SelectedNode=node;simulation.visualizer.InspectNode(node);}
        public void CloseInspection()
        {SetInspectedNode(null);simulation.selectedSegment=null;selectedObstacle=null;clearFocus=true;}
        public void InspectNode(PedestrianNode node,bool focus=false)
        {
            if(node==null || !simulation.graph.nodes.Contains(node))return;
            CancelPicking();CloseInspection();SetInspectedNode(node);detailScroll=Vector2.zero;
            if(focus){frameNextRoute=false;inspection.Stop();cameraController.FramePoint(node.Position);}
        }
        public void UseInspectedNodeAsEndpoint(bool origin)
        {
            if(SelectedNode==null)return;
            inspection.Stop();CancelPicking();
            if(origin)simulation.origin=SelectedNode;else simulation.destination=SelectedNode;
            simulation.RequestRecalculation();
        }
        public AccessibilitySegment[] ConnectedSegments(PedestrianNode node)
        {return simulation.graph.segments.Where(s=>s.start==node || s.end==node).ToArray();}
        private ScenarioDefinition CurrentScenario=>simulation.scenarios.scenarios[simulation.scenarios.currentIndex];
        private PedestrianNode ScenarioElevationNode=>CurrentScenario.elevationOffset!=0
            ?simulation.graph.nodes.Find(n=>n.id==CurrentScenario.raisedNodeId):null;
        private AccessibilitySegment ScenarioObstacleSegment
        {
            get
            {
                foreach(var obstacle in CurrentScenario.obstacles)
                {
                    var segment=simulation.graph.segments.Find(s=>s.id==obstacle.segmentId);
                    if(segment!=null)return segment;
                }
                return null;
            }
        }
        public bool HasScenarioInspectionTarget=>ScenarioObstacleSegment!=null || ScenarioElevationNode!=null;
        public void InspectScenarioChange()
        {
            var segment=ScenarioObstacleSegment;
            if(segment!=null)InspectSegment(segment);else if(ScenarioElevationNode!=null)InspectNode(ScenarioElevationNode,true);
        }
        private void DrawNodeInspector()
        {
            var node=SelectedNode;var edges=ConnectedSegments(node);
            DrawPanel(DetailPanel);GUILayout.BeginArea(DetailPanel,box);detailScroll=GUILayout.BeginScrollView(detailScroll);
            GUILayout.BeginHorizontal();GUILayout.Label("Conhecer o nó",heading);
            bool close=GUILayout.Button("×",button,GUILayout.Width(32));GUILayout.EndHorizontal();
            if(close){CloseInspection();GUILayout.EndScrollView();GUILayout.EndArea();return;}
            GUILayout.Label(node.displayName,body);GUILayout.Label("Identificador: "+node.id,small);
            GUILayout.Label("A bolinha azul representa um ponto de ligação da rede. As condições de passagem são avaliadas nos trechos conectados.",small);
            string role=node==simulation.origin && node==simulation.destination?"Origem A e destino B":node==simulation.origin?"Origem A":node==simulation.destination?"Destino B":"Ponto intermediário da rede";
            GUILayout.Label("Papel no percurso: "+role,body);
            var geo=simulation.geographicContext;
            if(geo!=null && node==geo.faesa)GUILayout.Label("Referência aproximada da FAESA na calçada. Não é uma entrada do campus verificada.",small);
            else if(geo!=null)GUILayout.Label("Ponto da rede experimental sobre a cartografia. Calçadas e conexões são aproximações.",small);
            else GUILayout.Label("Ponto de um mapa sintético usado para demonstração.",small);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Usar como A",button))UseInspectedNodeAsEndpoint(true);
            if(GUILayout.Button("Usar como B",button))UseInspectedNodeAsEndpoint(false);
            GUILayout.EndHorizontal();
            if(GUILayout.Button("Aproximar este nó",button)){inspection.Stop();cameraController.FramePoint(node.Position);}
            Gap();GUILayout.Label("ALTERAÇÃO DO CENÁRIO",small);
            GUILayout.Label(CurrentScenario.displayName,body);
            if(node==ScenarioElevationNode)
                GUILayout.Label($"Este é o nó escolhido pelo cenário para alterar a altura. Deslocamento previsto: {CurrentScenario.elevationOffset:+0.00;-0.00;0.00} m. Os trechos ligados a ele mudam de inclinação.",body);
            else GUILayout.Label("Este nó não é o alvo de elevação do cenário atual. Obstáculos e edições podem afetar os trechos ligados a ele.",small);
            Gap();GUILayout.Label("POSIÇÃO E ALTURA",small);
            var position=node.Position;
            GUILayout.Label($"Cota local atual: {position.y:F2} m\nX (leste): {position.x:F2} m\nZ (norte): {position.z:F2} m",body);
            GUILayout.Label("Coordenadas locais em metros; a cota não deve ser interpretada automaticamente como altitude sobre o nível do mar.",small);
            string state=node.elevationVerification==VerificationState.Simulated?"Simulada":node.elevationVerification==VerificationState.FieldVerified?"Verificada em campo":"Não verificada";
            GUILayout.Label("Situação da cota: "+state,small);
            if(!string.IsNullOrEmpty(node.elevationSource))GUILayout.Label("Fonte da cota: "+node.elevationSource,small);
            float baseline=simulation.scenarios.BaseElevation(node),difference=position.y-baseline;
            GUILayout.Label($"Cota-base: {baseline:F2} m\nDiferença atual para a base: {difference:+0.00;-0.00;0.00} m",body);
            GUILayout.Label("A diferença para a base mostra o estado atual e também pode incluir edições manuais. A base pode ter sido substituída por uma grade de cotas.",small);
            Gap();GUILayout.Label("TRECHOS CONECTADOS · "+edges.Length,small);
            GUILayout.Label("Perfil ativo: "+simulation.Profile.displayName,small);
            GUILayout.Label("Clique num trecho abaixo para aproximar e abrir o editor. A avaliação mostrada considera a saída a partir deste nó.",small);
            foreach(var edge in edges)
            {
                bool forward=edge.start==node;var other=forward?edge.end:edge.start;
                var evaluation=AccessibilityEvaluator.Evaluate(edge,simulation.Profile,forward);
                if(GUILayout.Button(edge.displayName+"\n"+edge.id+"\nAté "+other.displayName+" ("+other.id+")",button))
                {InspectSegment(edge);break;}
                GUI.contentColor=RouteVisualizer.RatingColor(evaluation.rating);
                GUILayout.Label(!forward && !edge.bidirectional?"Sem saída neste sentido":RatingLabel(evaluation.rating),small);GUI.contentColor=Color.white;
                GUILayout.Label($"Comprimento: {edge.Length:F2} m · faixa livre: {edge.EffectiveWidth:F2} m\nInclinação de saída: {(forward?edge.SlopePercent:-edge.SlopePercent):F2}%",small);
                GUILayout.Label(evaluation.reason,small);
                if(CurrentScenario.obstacles.Any(o=>o.segmentId==edge.id))GUILayout.Label("Trecho indicado pelo cenário para receber obstáculo simulado.",small);
            }
            Gap();if(GUILayout.Button("Voltar à comparação",button))CloseInspection();
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
    }
}
