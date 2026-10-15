using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed partial class SimulationUI
    {
        private AccessibilitySegment draftSegment;
        private AccessibilityObstacle draftObstacle;
        private int draftRevision=-1,addKind;
        private float free,total,startY,endY,cross,rough,amplitude,period,targetSlope;
        private bool assessed,open;
        private float obstacleWidth,obstacleLength,obstacleOffset,obstacleHeight,obstacleSeverity,obstaclePosition;
        private bool obstacleBypass,obstacleClosure;
        private string csvPath="",csvDatum="Plano artificial local",csvVerticalOrigin="0";
        private bool csvSimulated=true;
        private readonly Dictionary<string,string> numericFields=new Dictionary<string,string>();
        private Rect Toolbar=>Layout.toolbar;
        private void DrawWorldToolbar()
        {
            var editor=simulation.worldEditor;if(editor==null)return;
            DrawPanel(Toolbar);GUILayout.BeginArea(Toolbar,new GUIStyle{padding=new RectOffset(8,8,6,6)});
            GUILayout.BeginHorizontal();GUI.enabled=editor.CanUndo;if(GUILayout.Button("Desfazer",button))editor.Undo();
            GUI.enabled=editor.CanRedo;if(GUILayout.Button("Refazer",button))editor.Redo();GUI.enabled=true;
            if(GUILayout.Button("Salvar experiência",button))editor.Save();if(GUILayout.Button("Carregar experiência",button))editor.Load();GUILayout.EndHorizontal();
            DrawLandmarkNavigation();GUILayout.Label(editor.status,small);GUILayout.EndArea();
        }
        private void ReadDraft(AccessibilitySegment s)
        {
            draftSegment=s;draftObstacle=selectedObstacle;draftRevision=simulation.worldEditor.Revision;numericFields.Clear();
            total=s.totalWidth;free=s.freeWidth;cross=s.crossSlopePercent;rough=s.roughness;amplitude=s.surfaceAmplitude;period=s.surfacePeriod;
            startY=s.start.Position.y;endY=s.end.Position.y;targetSlope=s.SlopePercent;assessed=s.hasAssessmentData;open=s.networkOpen;
            if(selectedObstacle!=null)
            {
                var o=selectedObstacle;var footprint=ObstacleFootprints.Bounds(o);obstacleWidth=footprint.height;obstacleLength=o.length;
                obstacleOffset=footprint.center.y;obstacleHeight=o.heightOrDepth;obstacleSeverity=o.severity;obstaclePosition=o.positionAlongSegment;
                obstacleBypass=o.canBypass;obstacleClosure=o.blocksPassage;
            }
        }
        private void DrawWorldInspector()
        {
            var s=simulation.selectedSegment;var editor=simulation.worldEditor;
            if(selectedObstacle!=null && !selectedObstacle.isActiveAndEnabled)selectedObstacle=null;
            if(selectedObstacle!=null && selectedObstacle.segment!=s)selectedObstacle=null;
            if(draftSegment!=s || draftObstacle!=selectedObstacle || draftRevision!=editor.Revision)ReadDraft(s);
            DrawPanel(DetailPanel);GUILayout.BeginArea(DetailPanel,box);detailScroll=GUILayout.BeginScrollView(detailScroll);
            GUILayout.BeginHorizontal();GUILayout.Label("Editar ambiente",heading);if(GUILayout.Button("×",button,GUILayout.Width(32)))simulation.selectedSegment=null;GUILayout.EndHorizontal();
            GUILayout.Label(s.displayName,body);GUILayout.Label(s.id,small);
            if(!string.IsNullOrEmpty(s.sourceFeatureId))GUILayout.Label(s.sourceFeatureId+"\n"+s.geometryProvenance,small);
            var evaluation=AccessibilityEvaluator.Evaluate(s,simulation.Profile,true);
            GUI.contentColor=RouteVisualizer.RatingColor(evaluation.rating);GUILayout.Label(RatingLabel(evaluation.rating),body);GUI.contentColor=Color.white;
            GUILayout.Label(evaluation.reason,small);
            GUILayout.Label($"Distância: {s.Length:F2} m • horizontal: {s.HorizontalDistance:F2} m\nInclinação: {s.SlopePercent:F2}% • livre: {s.EffectiveWidth:F2} m",body);
            Gap();GUILayout.Label("RAMPA, COTAS E PAVIMENTO",small);
            GUILayout.Label("Alterar uma cota afeta todos os trechos ligados ao mesmo nó. As edições abaixo são simuladas.",small);
            startY=Number("Cota inicial local (m)",startY);endY=Number("Cota final local (m)",endY);targetSlope=Number("Inclinação alvo (%)",targetSlope);
            if(GUILayout.Button("Calcular cota final pela inclinação",button)){endY=startY+s.HorizontalDistance*targetSlope/100f;numericFields.Remove("Cota final local (m)");}
            total=Number("Largura total (m)",total);free=Slider("Largura livre base (m)",free,0,Mathf.Max(.1f,total));
            cross=Slider("Transversal (%)",cross,-10,10);rough=Slider("Irregularidade avaliada",rough,0,1);
            amplitude=Slider("Ondulação física (m)",amplitude,0,.05f);period=Slider("Período da ondulação (m)",period,.2f,10);
            assessed=GUILayout.Toggle(assessed,"Parâmetros de avaliação disponíveis");open=GUILayout.Toggle(open,"Trecho aberto na rede");
            bool numbersValid=numericFields.Values.All(text=>float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out float number) && !float.IsNaN(number) && !float.IsInfinity(number));
            if(!numbersValid)GUILayout.Label("Corrija os campos numéricos. Use ponto decimal.",body);
            GUI.enabled=numbersValid;
            if(GUILayout.Button("Aplicar trecho / rampa simulada",button))
            {
                if(total>=.1f && total<=100 && Mathf.Abs(startY)<=10000 && Mathf.Abs(endY)<=10000)
                    editor.Edit("Trecho/rampa atualizado",()=>
                    {
                        var ramp=s.GetComponent<AccessibilityRamp>()??s.gameObject.AddComponent<AccessibilityRamp>();ramp.segment=s;
                        ramp.startElevation=startY;ramp.endElevation=endY;ramp.width=total;ramp.crossSlopePercent=cross;ramp.Apply();
                        s.freeWidth=Mathf.Clamp(free,0,total);s.roughness=rough;s.surfaceAmplitude=amplitude;s.surfacePeriod=period;s.hasAssessmentData=assessed;s.networkOpen=open;
                        foreach(var edge in simulation.graph.segments)if(edge.start==s.start || edge.start==s.end || edge.end==s.start || edge.end==s.end)edge.elevationIsSimulated=true;
                    });
                else editor.status="Cotas ou largura fora da faixa admitida.";
            }
            GUI.enabled=true;
            Gap();GUILayout.Label("OBSTÁCULOS SIMULADOS",small);
            foreach(var obstacle in s.obstacles.ToArray())
                if(obstacle!=null && obstacle.isActiveAndEnabled && GUILayout.Button(KindLabel(obstacle.kind),button))selectedObstacle=obstacle;
            if(selectedObstacle!=null)
            {
                var o=selectedObstacle;GUILayout.Label("Editar "+KindLabel(o.kind),body);
                obstacleWidth=Slider("Largura ocupada (m)",obstacleWidth,.05f,s.totalWidth);
                obstacleLength=Slider("Comprimento ocupado (m)",obstacleLength,.05f,Mathf.Min(10,Mathf.Max(.05f,s.HorizontalDistance)));
                obstacleOffset=Slider("Deslocamento lateral (m)",obstacleOffset,-s.totalWidth*.5f,s.totalWidth*.5f);
                obstaclePosition=Slider("Posição longitudinal (0–1)",obstaclePosition,.02f,.98f);
                obstacleHeight=Slider(o.kind==ObstacleKind.Pothole?"Profundidade (m)":"Altura (m)",obstacleHeight,0,o.kind==ObstacleKind.Pothole?.25f:4);
                obstacleSeverity=Slider("Severidade",obstacleSeverity,0,1);
                obstacleBypass=GUILayout.Toggle(obstacleBypass,"Permite contorno");obstacleClosure=GUILayout.Toggle(obstacleClosure,"Interdita passagem sem contorno");
                if(GUILayout.Button("Aplicar obstáculo",button))editor.Edit("Obstáculo atualizado",()=>
                {
                    o.useFootprintForClearance=true;o.occupiedWidth=obstacleWidth;o.length=obstacleLength;o.lateralOffset=obstacleOffset;
                    o.positionAlongSegment=obstaclePosition;o.heightOrDepth=obstacleHeight;o.severity=obstacleSeverity;o.canBypass=obstacleBypass;o.blocksPassage=obstacleClosure;
                    o.remainingFreeWidth=Mathf.Max(0,s.totalWidth-obstacleWidth);
                });
                if(GUILayout.Button("Remover obstáculo",button)){editor.Edit("Obstáculo removido",()=>simulation.scenarios.RemoveObstacle(o));selectedObstacle=null;}
            }
            addKind=GUILayout.SelectionGrid(addKind,new[]{"Poste","Árvore","Lixeira","Mobiliário","Objeto","Degrau","Buraco","Obras"},2,button);
            var kinds=new[]{ObstacleKind.Pole,ObstacleKind.Tree,ObstacleKind.Bin,ObstacleKind.Furniture,ObstacleKind.Generic,ObstacleKind.Step,ObstacleKind.Pothole,ObstacleKind.Construction};
            if(GUILayout.Button("Adicionar tipo selecionado",button))selectedObstacle=editor.AddObstacle(s,kinds[addKind]);
            GUILayout.Label("A largura livre é o maior intervalo contínuo restante, considerando os obstáculos que se sobrepõem longitudinalmente.",small);
            Gap();GUILayout.Label("GRADE DE ELEVAÇÃO LOCAL",small);GUILayout.Label(editor.elevationDescription,small);
            if(GUILayout.Button("Aplicar grade artificial de demonstração",button))editor.ApplyElevation(editor.exampleElevation);
            GUILayout.Label("CSV local: x,z,elevation. Use metros, ponto decimal e a mesma origem X/Z da cena. As cotas representam a superfície de circulação.",small);
            GUI.SetNextControlName("edit-csvPath");csvPath=GUILayout.TextField(csvPath);GUILayout.Label("Datum vertical / referência",small);GUI.SetNextControlName("edit-csvDatum");csvDatum=GUILayout.TextField(csvDatum);
            GUILayout.Label("Origem vertical a subtrair (m)",small);GUI.SetNextControlName("edit-csvOrigin");csvVerticalOrigin=GUILayout.TextField(csvVerticalOrigin);
            csvSimulated=GUILayout.Toggle(csvSimulated,"Arquivo contém dados simulados");
            if(GUILayout.Button("Carregar e aplicar CSV local",button))
            {
                if(double.TryParse(csvVerticalOrigin,NumberStyles.Float,CultureInfo.InvariantCulture,out var origin))editor.LoadElevationCsv(csvPath,csvDatum,origin,csvSimulated);
                else editor.status="Origem vertical inválida; use ponto decimal.";
            }
            GUILayout.Label("Dados importados são não verificados. Fora da cobertura, cotas anteriores permanecem. A grade atual persiste ao alternar cenários; desfazer restaura a base anterior.",small);
            Gap();GUILayout.Label("PERFIL ATUAL",small);var p=simulation.Profile;
            GUILayout.Label($"Largura: {p.requiredWidth:F2} m • degrau: {p.maxStepHeight:F2} m\nSubida/descida: {p.maxUphillPercent:F1}% / {p.maxDownhillPercent:F1}%\nTransversal: {p.maxCrossSlopePercent:F1}%",body);
            GUILayout.EndScrollView();GUILayout.EndArea();
        }
        private float Number(string label,float value)
        {
            GUILayout.Label(label,small);if(!numericFields.TryGetValue(label,out var text))text=value.ToString("G7",CultureInfo.InvariantCulture);
            GUI.SetNextControlName("edit-"+label);text=GUILayout.TextField(text);numericFields[label]=text;
            return float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out var parsed) && !float.IsNaN(parsed) && !float.IsInfinity(parsed)?parsed:value;
        }
        private static string KindLabel(ObstacleKind kind)
        {
            switch(kind){case ObstacleKind.Pole:return "Poste";case ObstacleKind.Tree:return "Árvore";case ObstacleKind.Bin:return "Lixeira";case ObstacleKind.Furniture:return "Mobiliário";case ObstacleKind.Step:return "Degrau";case ObstacleKind.Pothole:return "Buraco";case ObstacleKind.Construction:return "Obras";default:return "Objeto genérico";}
        }
    }
}
