using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrajetoInclusivo
{
    public sealed partial class SimulationUI : MonoBehaviour
    {
        public SimulationController simulation;
        public CameraController cameraController;
        public RouteInspection inspection;
        private int picking; // 1 origin, 2 destination
        private Vector2 scroll, detailScroll;
        private GUIStyle heading, small, body, button, box;
        private AccessibilityObstacle selectedObstacle;
        private SimulationLayout Layout=>new SimulationLayout(Screen.width,Screen.height);
        private float UIScale => Layout.scale;
        private float VirtualWidth => Screen.width / UIScale;
        private float VirtualHeight => Screen.height / UIScale;
        private Rect LeftPanel => Layout.left;
        private Rect DetailPanel => Layout.right;
        public Rect SceneViewport=>Layout.viewport;
        public bool TextInputFocused{get;private set;}
        public bool PickingEndpoint=>picking!=0;
        private bool clearFocus;
        public void BeginPicking(bool origin)
        {CloseInspection();picking=origin?1:2;inspection.Stop();clearFocus=true;}
        public void CancelPicking(){picking=0;clearFocus=true;}
        public void InspectSegment(AccessibilitySegment segment,bool focus=true)
        {CloseInspection();picking=0;simulation.selectedSegment=segment;detailScroll=Vector2.zero;clearFocus=true;if(focus){inspection.Stop();cameraController.FrameSegment(segment);}}
        public bool HandleMapRay(Ray ray)
        {
            if(!Physics.Raycast(ray,out var hit,10000))return false;
            var node=hit.collider.GetComponentInParent<PedestrianNode>();
            if(picking!=0)
            {
                if(node==null || !simulation.graph.nodes.Contains(node))return false;
                if(picking==1)simulation.origin=node;else simulation.destination=node;
                CancelPicking();simulation.RequestRecalculation();return true;
            }
            if(node!=null && simulation.graph.nodes.Contains(node)){InspectNode(node);return true;}
            var obstacle=hit.collider.GetComponentInParent<AccessibilityObstacle>();
            var segment=obstacle!=null?obstacle.segment:hit.collider.GetComponentInParent<AccessibilitySegment>();
            if(obstacle==null && segment!=null)
            {
                var local=ObstacleFootprints.LocalPoint(segment,hit.point);
                obstacle=segment.obstacles.FirstOrDefault(o=>o!=null && o.isActiveAndEnabled && o.kind==ObstacleKind.Pothole && ObstacleFootprints.Bounds(o).Contains(local));
            }
            if(segment==null)return false;
            SetInspectedNode(null);
            if(simulation.selectedSegment!=segment)detailScroll=Vector2.zero;
            simulation.selectedSegment=segment;selectedObstacle=obstacle;return true;
        }
        public bool ContainsPointer()
        {return IsPointerOverUI(Input.mousePosition);}
        public bool IsPointerOverUI(Vector2 screenPosition)
        {
            Vector2 p = new Vector2(screenPosition.x, Screen.height-screenPosition.y) / UIScale;
            if(!Layout.map.Contains(p))return true;
            return GetLandmarkMarkers().Any(marker=>marker.rect.Contains(p));
        }
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape)){CancelPicking();CloseInspection();}
            if (!Input.GetMouseButtonDown(0) || ContainsPointer()) return;
            clearFocus=true;HandleMapRay(cameraController.view.ScreenPointToRay(Input.mousePosition));
        }
        private void Styles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, wordWrap = true };
            body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 12 };
            small.normal.textColor = new Color(.67f, .76f, .82f);
            button = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true, padding = new RectOffset(8, 8, 8, 8) };
            box = new GUIStyle { padding = new RectOffset(18, 18, 16, 16) };
        }
        private void OnGUI()
        {
            Styles(); GUI.matrix = Matrix4x4.Scale(Vector3.one * UIScale);
            DrawViewportSurround();
            if(clearFocus){GUI.FocusControl(null);TextInputFocused=false;clearFocus=false;}
            DrawPanel(LeftPanel);
            GUILayout.BeginArea(LeftPanel, box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUI.contentColor = new Color(.36f, .9f, .8f);
            GUILayout.Label("TRAJETO INCLUSIVO", small); GUI.contentColor = Color.white;
            GUILayout.Label("Laboratório de\nacessibilidade urbana", heading);
            var geo = simulation.geographicContext;
            GUILayout.Label(geo == null ? "MVP • REDE 100% SINTÉTICA" : geo.faesa!=null?"PRAÇA · IFES · FAESA • BASE OSM":"AVENIDA VITÓRIA • BASE OSM", body);
            GUILayout.Label(geo == null ? "Este mapa sintético não representa a região real."
                : "Eixos e polígonos importados. Calçadas, larguras, cotas e conexões são aproximações de teste.", small);
            if (Application.CanStreamedLevelBeLoaded("AvenidaVitoria_Geografico"))
            {
                if (GUILayout.Button(geo == null ? "Abrir mapa da Avenida Vitória" : "Abrir testbed sintético", button))
                    SceneManager.LoadScene(geo == null ? "AvenidaVitoria_Geografico" : "AvenidaVitoria_Testbed");
            }
            if(Application.CanStreamedLevelBeLoaded("AvenidaVitoria_FAESA") && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="AvenidaVitoria_FAESA")
                if(GUILayout.Button("Abrir mapa ampliado até a FAESA",button))SceneManager.LoadScene("AvenidaVitoria_FAESA");
            if(geo!=null && geo.faesa!=null && GUILayout.Button("Abrir recorte original Praça–Ifes",button))SceneManager.LoadScene("AvenidaVitoria_Geografico");
            if (geo != null)
            {
                bool hypotheses = GUILayout.Toggle(geo.useSimulatedAssessment, "Ativar hipóteses de teste (SIMULAÇÃO)");
                if (hypotheses != geo.useSimulatedAssessment) simulation.worldEditor.Edit("Hipóteses de avaliação alteradas",()=>geo.SetAssessmentMode(hypotheses, simulation));
                if (!geo.useSimulatedAssessment) GUILayout.Label("Sem levantamento: a rede permanece não avaliada para acessibilidade.", small);
                else GUILayout.Label("Avaliação SIMULADA ativada. Não representa condições verificadas no local.",small);
            }
            Gap(); GUILayout.Label("01  CONDIÇÃO ARTIFICIAL", small);
            var scenarios = simulation.scenarios;
            string[] names = scenarios.scenarios.Select(s => s.displayName).ToArray();
            int scenario = GUILayout.SelectionGrid(scenarios.currentIndex, names, 1, button);
            if (scenario != scenarios.currentIndex)
            {
                selectedObstacle = null; inspection.Stop(); simulation.worldEditor.Edit("Cenário alterado",()=>scenarios.Apply(scenario));
            }
            GUILayout.Label(scenarios.scenarios[scenarios.currentIndex].description, small);
            if(HasScenarioInspectionTarget && GUILayout.Button("Localizar alteração do cenário",button))InspectScenarioChange();
            Gap(); GUILayout.Label("02  PERFIL DE MOBILIDADE", small);
            int profile = GUILayout.SelectionGrid(simulation.profileIndex, simulation.profiles.Select(p => p.displayName).ToArray(), 1, button);
            if (profile != simulation.profileIndex) simulation.SetProfile(profile);
            GUILayout.Label("Hipóteses ajustáveis; não é laudo ou verificação da NBR 9050.", small);
            if(GUILayout.Button("Consultar limites dos perfis",button))ShowProfileComparison(true);
            Gap(); GUILayout.Label("03  ORIGEM E DESTINO", small);
            GUILayout.Label($"A (anel verde): {simulation.origin.displayName}\nB (anel violeta): {simulation.destination.displayName}", body);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Escolher A", button)) BeginPicking(true);
            if (GUILayout.Button("Escolher B", button)) BeginPicking(false);
            if (GUILayout.Button("Inverter", button))
            {
                var old = simulation.origin; simulation.origin = simulation.destination; simulation.destination = old;
                simulation.RequestRecalculation();
            }
            GUILayout.EndHorizontal();
            if(geo!=null && geo.faesa!=null)
            {
                if(GUILayout.Button("Praça → FAESA",button))SetFaesaRoute(false);
                if(GUILayout.Button("Ifes → FAESA",button))SetFaesaRoute(true);
            }
            if (picking != 0){GUILayout.Label("Clique em uma esfera no mapa para definir " + (picking == 1 ? "A." : "B.")+" Esc cancela.", body);if(GUILayout.Button("Cancelar seleção",button))CancelPicking();}
            Gap(); GUILayout.Label("04  COMPARAÇÃO DAS ROTAS", small);
            bool accessible = GUILayout.SelectionGrid(simulation.showAccessible ? 1 : 0, new[] { "Convencional", "Acessível" }, 2, button) == 1;
            if (accessible != simulation.showAccessible)
            {
                simulation.showAccessible = accessible; simulation.visualizer.Refresh(simulation);
            }
            GUILayout.Label("Verde: acessível  •  Amarelo: dificuldade\nVermelho: inacessível  •  Cinza: não avaliado\nA rota selecionada tem linha mais espessa.", small);
            Gap(); GUILayout.BeginHorizontal();
            if (GUILayout.Button(cameraController.topDown ? "Perspectiva 3D" : "Vista superior", button)) cameraController.ToggleView();
            if (GUILayout.Button("Enquadrar", button)) { inspection.Stop(); cameraController.FrameMap(); }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Focar percurso disponível", button))
            {
                inspection.Stop();
                cameraController.FrameRoute(simulation.showAccessible && simulation.accessible != null && simulation.accessible.Found ? simulation.accessible : simulation.conventional);
            }
            GUI.enabled = simulation.accessible != null && simulation.accessible.Found;
            if (GUILayout.Button(inspection.Running ? "Parar inspeção" : "Acompanhar rota acessível", button))
            { if (inspection.Running) inspection.Stop(); else inspection.Begin(); }
            GUI.enabled = true;
            GUILayout.Label("WASD: deslocar • Q/E: altura • botão direito: orbitar\nRoda: zoom • T: vista • Home: enquadrar\nClique numa bolinha azul para conhecer o nó; na calçada ou no obstáculo para editar.", small);
            GUILayout.EndScrollView(); GUILayout.EndArea();
            if (simulation.selectedSegment != null) DrawInspector(); else if(SelectedNode!=null)DrawNodeInspector();else DrawComparison();
            DrawWorldToolbar();
            DrawLandmarkMarkers();
            if (geo != null)
            {
                var footer = Layout.footer;
                DrawPanel(footer);
                if (GUI.Button(footer, geo.definition.attribution + "\nDados: " + geo.definition.datasetDate + " · Calçadas e condições simuladas", small))
                    Application.OpenURL("https://www.openstreetmap.org/copyright");
            }
            TextInputFocused=(GUI.GetNameOfFocusedControl()??"").StartsWith("edit-");
        }
        private void DrawComparison()
        {
            DrawPanel(DetailPanel); GUILayout.BeginArea(DetailPanel, box);
            detailScroll = GUILayout.BeginScrollView(detailScroll);
            GUILayout.Label("Comparar trajetos", heading);
            bool profileTab=GUILayout.SelectionGrid(showProfiles?1:0,new[]{"Trajetos","Três perfis"},2,button)==1;
            if(profileTab!=showProfiles){showProfiles=profileTab;detailScroll=Vector2.zero;}
            if(showProfiles){DrawProfiles();GUILayout.EndScrollView();GUILayout.EndArea();return;}
            var comparison=simulation.comparison;
            GUILayout.Label(simulation.Profile.displayName,body);
            GUILayout.Label("Distância usa comprimento 3D entre nós. O custo acessível é ponderado por dificuldade, sem unidade física de esforço.", small);
            if (simulation.geographicContext != null) GUILayout.Label("Rotas experimentais sobre calçadas e conexões aproximadas. Cotas simuladas ou importadas não verificadas; sem levantamento de campo.", small);
            if(comparison==null){GUILayout.Label("Calculando…",body);GUILayout.EndScrollView();GUILayout.EndArea();return;}
            if(comparison.HasBoth)GUILayout.Label($"Desvio: +{comparison.ExtraDistance:F1} m ({comparison.ExtraPercent:F1}%)",heading);
            DrawComparisonMetrics(comparison);
            GUILayout.Label(comparison.explanation,body);
            RouteCard("CONVENCIONAL · LINHA AZUL",comparison.conventional,false);
            Gap(); RouteCard("ACESSÍVEL · LINHA VERDE/AMARELA",comparison.accessible,true);
            Gap();if(GUILayout.Button("Comparar os três perfis",button))ShowProfileComparison(true);
            Gap(); GUILayout.Label("Clique numa bolinha azul para conhecer o nó ou numa calçada para editar o ambiente.", body);
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
        private void RouteCard(string title, RouteAnalysis analysis,bool accessible)
        {
            var route=analysis.route;
            GUILayout.Space(9); GUILayout.Label(title, small);
            if (route == null) { GUILayout.Label("Calculando…", body); return; }
            if (!route.Found) { GUILayout.Label(FailureLabel(route.failure),body);GUILayout.Label(route.explanation, small); return; }
            GUILayout.Label($"{analysis.difficultCount} difícil(is) · {analysis.blockedCount} impedido(s) · {analysis.unknownCount} não avaliado(s)",small);
            var issues=analysis.segments.Where(s=>s.rating!=AccessibilityRating.Accessible).ToList();
            bool expanded=accessible?expandAccessible:expandConventional;
            foreach (var item in issues.Take(expanded?issues.Count:4))
            {
                GUI.contentColor=RouteVisualizer.RatingColor(item.rating);
                GUILayout.Label(RatingLabel(item.rating),small);
                GUI.contentColor=Color.white;
                if(GUILayout.Button(item.segment.displayName+" · "+(item.forward?"ida":"volta")+"\n"+item.reason,button))InspectSegment(item.segment);
            }
            if(issues.Count>4 && GUILayout.Button(expanded?"Mostrar menos":$"Ver os {issues.Count} trechos problemáticos",button))
            {if(accessible)expandAccessible=!expandAccessible;else expandConventional=!expandConventional;}
            if(route.arcs.Count==0)GUILayout.Label("Origem e destino coincidem.",small);
        }
        private void DrawInspector() => DrawWorldInspector();
        public void SetFaesaRoute(bool fromIfes)
        {
            var context=simulation.geographicContext;if(context==null || context.faesa==null)return;
            CancelPicking();inspection.Stop();CloseInspection();
            simulation.origin=fromIfes?context.ifes:context.plaza;simulation.destination=context.faesa;simulation.RequestRecalculation();
            frameNextRoute=true;
        }
        private float Slider(string label, float value, float min, float max)
        {
            GUILayout.Label(label + ": " + value.ToString("F2"), small);
            return GUILayout.HorizontalSlider(value, min, max);
        }
        private static string RatingLabel(AccessibilityRating r)
        {
            switch (r) { case AccessibilityRating.Accessible: return "Acessível no modelo"; case AccessibilityRating.Difficult: return "Acessível com dificuldade"; case AccessibilityRating.Inaccessible: return "Inacessível para este perfil"; default: return "Não avaliado"; }
        }
        private static void Gap() => GUILayout.Space(16);
        private void DrawViewportSurround()
        {
            var area=Layout.map;var oldColor=GUI.color;GUI.color=new Color(.025f,.035f,.045f,1);
            GUI.DrawTexture(new Rect(0,0,VirtualWidth,area.yMin),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,area.yMax,VirtualWidth,VirtualHeight-area.yMax),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0,area.yMin,area.xMin,area.height),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.xMax,area.yMin,VirtualWidth-area.xMax,area.height),Texture2D.whiteTexture);
            GUI.color=oldColor;
        }
        private static void DrawPanel(Rect rect)
        { GUI.color = new Color(.055f, .085f, .12f, 1); GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = Color.white; }
    }
}
