using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrajetoInclusivo
{
    public sealed class RuntimeWorldEditor:MonoBehaviour
    {
        public SimulationController simulation;
        public string status="Alterações de geometria e obstáculos são hipóteses de teste.";
        public string elevationDescription="Cotas artificiais do cenário";
        public ElevationDataset exampleElevation;
        public string Fingerprint{get;private set;}
        public int Revision{get;private set;}
        private string activeElevationJson="";
        private ElevationSurfaceLayer terrainLayer;
        private readonly List<string> undo=new List<string>(),redo=new List<string>();
        public bool CanUndo=>undo.Count>0;
        public bool CanRedo=>redo.Count>0;
        public string DefaultSavePath=>Path.Combine(Application.persistentDataPath,SceneManager.GetActiveScene().name+".experiment.json");
        public void Initialize(SimulationController sim)
        {
            simulation=sim;exampleElevation=Resources.Load<ElevationDataset>("ExampleElevation");
            terrainLayer=gameObject.AddComponent<ElevationSurfaceLayer>();terrainLayer.Initialize(sim);
            var text=new StringBuilder(SceneManager.GetActiveScene().name);
            foreach(var n in sim.graph.nodes)text.Append('|').Append(n.id).Append(':').Append(n.Position.x.ToString("R",CultureInfo.InvariantCulture)).Append(',').Append(n.Position.z.ToString("R",CultureInfo.InvariantCulture));
            foreach(var s in sim.graph.segments)text.Append('|').Append(s.id).Append(':').Append(s.start.id).Append('>').Append(s.end.id);
            if(sim.geographicContext!=null)text.Append(sim.geographicContext.definition.provenance.text);
            using var hash=SHA256.Create();Fingerprint=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
        }
        public string CaptureJson()=>JsonUtility.ToJson(Capture(),false);
        public WorldState Capture()
        {
            var sim=simulation;
            return new WorldState
            {
                graphFingerprint=Fingerprint,originId=sim.origin.id,destinationId=sim.destination.id,profileIndex=sim.profileIndex,
                scenarioIndex=sim.scenarios.currentIndex,useSimulatedAssessment=sim.geographicContext!=null && sim.geographicContext.useSimulatedAssessment,elevationDescription=elevationDescription,
                elevationJson=activeElevationJson,
                nodes=sim.graph.nodes.Select(n=>new NodeState{id=n.id,height=n.Position.y,baseHeight=sim.scenarios.BaseElevation(n),source=n.elevationSource,verification=n.elevationVerification,
                    baseSource=sim.scenarios.BaseElevationSource(n).source,baseVerification=sim.scenarios.BaseElevationSource(n).state}).ToArray(),
                segments=sim.graph.segments.Select(s=>new SegmentState{id=s.id,totalWidth=s.totalWidth,freeWidth=s.freeWidth,crossSlope=s.crossSlopePercent,roughness=s.roughness,stepHeight=s.stepHeight,
                    amplitude=s.surfaceAmplitude,period=s.surfacePeriod,hasData=s.hasAssessmentData,open=s.networkOpen,elevationIsSimulated=s.elevationIsSimulated}).ToArray(),
                obstacles=sim.graph.segments.SelectMany(s=>s.obstacles.Where(o=>o!=null && o.isActiveAndEnabled).Select(o=>new ObstacleState{segmentId=s.id,kind=o.kind,position=o.positionAlongSegment,
                    length=o.length,width=o.occupiedWidth,lateral=o.lateralOffset,freeWidth=o.remainingFreeWidth,height=o.heightOrDepth,severity=o.severity,footprint=o.useFootprintForClearance,bypass=o.canBypass,closure=o.blocksPassage})).ToArray()
            };
        }
        public bool Edit(string description,Action action)
        {
            string before=CaptureJson();
            try{action();WorldStateValidation.Validate(Capture(),simulation,Fingerprint);Push(undo,before);redo.Clear();Revision++;simulation.RequestRecalculation();status=description+" — alteração aplicada.";return true;}
            catch(Exception e){Restore(before);status="Alteração recusada: "+e.Message;return false;}
        }
        public void Restore(string json)
        {
            var state=JsonUtility.FromJson<WorldState>(json);WorldStateValidation.Validate(state,simulation,Fingerprint);
            var nodes=simulation.graph.nodes.ToDictionary(n=>n.id);var segments=simulation.graph.segments.ToDictionary(s=>s.id);
            foreach(var s in segments.Values)
            {
                foreach(var o in s.obstacles.ToArray())if(o!=null){o.gameObject.SetActive(false);Destroy(o.gameObject);}
                s.obstacles.Clear();
            }
            foreach(var n in state.nodes)
            {
                var node=nodes[n.id];var p=node.Position;p.y=n.height;node.transform.position=p;node.elevationVerification=n.verification;node.elevationSource=n.source;
                simulation.scenarios.SetBaseElevation(node,n.baseHeight,n.baseVerification,n.baseSource);
            }
            foreach(var s in state.segments)
            {
                var target=segments[s.id];target.totalWidth=s.totalWidth;target.freeWidth=s.freeWidth;target.crossSlopePercent=s.crossSlope;target.roughness=s.roughness;target.stepHeight=s.stepHeight;
                target.surfaceAmplitude=s.amplitude;target.surfacePeriod=s.period;target.hasAssessmentData=s.hasData;target.networkOpen=s.open;target.elevationIsSimulated=s.elevationIsSimulated;
                var ramp=target.GetComponent<AccessibilityRamp>();if(ramp!=null)ramp.ReadFromSegment();
            }
            foreach(var o in state.obstacles)
            {
                var target=simulation.scenarios.CreateObstacle(segments[o.segmentId],o.kind);target.positionAlongSegment=o.position;target.length=o.length;target.occupiedWidth=o.width;
                target.lateralOffset=o.lateral;target.remainingFreeWidth=o.freeWidth;target.heightOrDepth=o.height;target.severity=o.severity;target.useFootprintForClearance=o.footprint;target.canBypass=o.bypass;target.blocksPassage=o.closure;
            }
            simulation.origin=nodes[state.originId];simulation.destination=nodes[state.destinationId];simulation.profileIndex=state.profileIndex;simulation.scenarios.currentIndex=state.scenarioIndex;
            if(simulation.geographicContext!=null)simulation.geographicContext.useSimulatedAssessment=state.useSimulatedAssessment;
            elevationDescription=state.elevationDescription;simulation.RequestRecalculation();
            SetElevationSurface(state.elevationJson??"");Revision++;
        }
        public void Undo(){if(!CanUndo)return;string next=undo[undo.Count-1];undo.RemoveAt(undo.Count-1);Push(redo,CaptureJson());Restore(next);status="Alteração desfeita.";}
        public void Redo(){if(!CanRedo)return;string next=redo[redo.Count-1];redo.RemoveAt(redo.Count-1);Push(undo,CaptureJson());Restore(next);status="Alteração refeita.";}
        private static void Push(List<string> history,string value){history.Add(value);if(history.Count>12)history.RemoveAt(0);}
        public bool Save(string path=null)
        {
            try
            {
                path=path??DefaultSavePath;Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                string temporary=path+".tmp";File.WriteAllText(temporary,CaptureJson(),Encoding.UTF8);
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
                status="Experiência salva: "+path;return true;
            }catch(Exception e){status="Não foi possível salvar: "+e.Message;return false;}
        }
        public bool Load(string path=null)
        {
            try
            {
                path=path??DefaultSavePath;if(new FileInfo(path).Length>8*1024*1024)throw new FormatException("Arquivo de experiência excessivo.");
                string json=File.ReadAllText(path);var state=JsonUtility.FromJson<WorldState>(json);WorldStateValidation.Validate(state,simulation,Fingerprint);
                return Edit("Experiência carregada",()=>Restore(json));
            }catch(Exception e){status="Carga recusada: "+e.Message;return false;}
        }
        public bool ApplyElevation(ElevationDataset dataset)
        {
            if(dataset==null){status="Selecione uma grade de elevação.";return false;}
            try{ElevationDataImporter.Validate(dataset);}catch(Exception e){status=e.Message;return false;}
            var samples=new Dictionary<PedestrianNode,float>();foreach(var n in simulation.graph.nodes)if(dataset.TrySample(n.Position.x,n.Position.z,out var h))samples[n]=h;
            if(samples.Count==0){status="A grade não cobre nenhum nó. Confira a origem local e os eixos X/Z.";return false;}
            return Edit("Grade aplicada a "+samples.Count+" nó(s)",()=>
            {
                var scenario=simulation.scenarios.scenarios[simulation.scenarios.currentIndex];
                foreach(var sample in samples)
                {
                    var n=sample.Key;var p=n.Position;p.y=sample.Value;
                    simulation.scenarios.SetBaseElevation(n,p.y,dataset.verification,dataset.sourceDescription);
                    n.elevationVerification=dataset.verification;n.elevationSource=dataset.sourceDescription;
                    if(n.id==scenario.raisedNodeId && scenario.elevationOffset!=0){p.y+=scenario.elevationOffset;n.elevationVerification=VerificationState.Simulated;n.elevationSource="Grade + alteração artificial de cenário";}
                    n.transform.position=p;
                }
                foreach(var s in simulation.graph.segments)s.elevationIsSimulated=s.start.elevationVerification==VerificationState.Simulated || s.end.elevationVerification==VerificationState.Simulated;
                elevationDescription=dataset.sourceDescription+" · "+dataset.verticalDatum+" · "+dataset.verification;
                SetElevationSurface(JsonUtility.ToJson(dataset));
            });
        }
        public bool LoadElevationCsv(string path,string datum,double verticalOrigin,bool simulated)
        {
            ElevationDataset dataset=null;
            try
            {
                if(new FileInfo(path).Length>8*1024*1024)throw new FormatException("CSV excessivo.");
                dataset=ElevationDataImporter.ParseCsv(File.ReadAllText(path),Path.GetFileName(path),datum,verticalOrigin,simulated);
                return ApplyElevation(dataset);
            }catch(Exception e){status="Grade recusada: "+e.Message;return false;}
            finally{if(dataset!=null)Destroy(dataset);}
        }
        public AccessibilityObstacle AddObstacle(AccessibilitySegment segment,ObstacleKind kind)
        {
            if(segment.obstacles.Count>=32){status="Limite de 32 obstáculos por trecho atingido.";return null;}
            if(kind==ObstacleKind.Pothole && segment.syntheticConnection && !simulation.scenarios.map.generateConnectionSurfaces){status="Esta conexão é uma guia hipotética. Selecione um trecho com pavimento para escavar um buraco.";return null;}
            AccessibilityObstacle created=null;
            bool applied=Edit("Obstáculo adicionado",()=>
            {
                created=simulation.scenarios.CreateObstacle(segment,kind);created.useFootprintForClearance=true;
                created.occupiedWidth=kind==ObstacleKind.Step?segment.totalWidth:Mathf.Min(.8f,segment.totalWidth);
                created.lateralOffset=(segment.totalWidth-created.occupiedWidth)*.5f;
                created.heightOrDepth=kind==ObstacleKind.Pole?1.8f:kind==ObstacleKind.Tree?2.8f:kind==ObstacleKind.Pothole?.12f:kind==ObstacleKind.Construction?1.1f:.16f;
                if(kind==ObstacleKind.Construction){created.occupiedWidth=segment.totalWidth;created.canBypass=false;created.blocksPassage=true;}
            });return applied?created:null;
        }
        public bool ConfigureRamp(AccessibilitySegment segment,float start,float end,float width,float cross)
        {
            if(!Finite(start) || !Finite(end) || !Finite(width) || !Finite(cross) || Mathf.Abs(start)>10000 || Mathf.Abs(end)>10000 || width<.1f || width>100 || Mathf.Abs(cross)>100){status="Parâmetros de rampa inválidos.";return false;}
            return Edit("Rampa/cotas aplicadas",()=>
            {
                var ramp=segment.GetComponent<AccessibilityRamp>()??segment.gameObject.AddComponent<AccessibilityRamp>();
                ramp.segment=segment;ramp.startElevation=start;ramp.endElevation=end;ramp.width=width;ramp.crossSlopePercent=cross;ramp.Apply();
                foreach(var s in simulation.graph.segments)if(s.start==segment.start || s.end==segment.start || s.start==segment.end || s.end==segment.end)s.elevationIsSimulated=true;
            });
        }
        private static bool Finite(float value)=>!float.IsInfinity(value) && !float.IsNaN(value);
        private void SetElevationSurface(string json)
        {
            if(activeElevationJson==json)return;
            ElevationDataset data=null;
            try
            {
                if(json.Length>0){data=ScriptableObject.CreateInstance<ElevationDataset>();JsonUtility.FromJsonOverwrite(json,data);}
                terrainLayer.Apply(data);activeElevationJson=json;
            }
            finally{if(data!=null)Destroy(data);}
        }
    }
}
