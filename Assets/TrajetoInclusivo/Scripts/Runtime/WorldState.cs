using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrajetoInclusivo
{
    [Serializable]public sealed class WorldState
    {
        public int schemaVersion=1;
        public string graphFingerprint,originId,destinationId,elevationDescription;
        public string elevationJson;
        public int profileIndex,scenarioIndex;
        public bool useSimulatedAssessment;
        public NodeState[] nodes;
        public SegmentState[] segments;
        public ObstacleState[] obstacles;
    }
    [Serializable]public sealed class NodeState
    {
        public string id,source,baseSource;
        public float height,baseHeight;
        public VerificationState verification,baseVerification;
    }
    [Serializable]public sealed class SegmentState
    {
        public string id;
        public float totalWidth,freeWidth,crossSlope,roughness,stepHeight,amplitude,period;
        public bool hasData,open,elevationIsSimulated;
    }
    [Serializable]public sealed class ObstacleState
    {
        public string segmentId;
        public ObstacleKind kind;
        public float position,length,width,lateral,freeWidth,height,severity;
        public bool footprint,bypass,closure;
    }
    public static class WorldStateValidation
    {
        public static void Validate(WorldState state,SimulationController sim,string fingerprint)
        {
            if(state==null || state.schemaVersion!=1 || state.graphFingerprint!=fingerprint)throw new FormatException("Experiência incompatível com esta versão do mapa/grafo.");
            if(!string.IsNullOrEmpty(state.elevationJson))
            {
                var data=ScriptableObject.CreateInstance<ElevationDataset>();
                try{JsonUtility.FromJsonOverwrite(state.elevationJson,data);ElevationDataImporter.Validate(data);}
                finally{if(Application.isPlaying)UnityEngine.Object.Destroy(data);else UnityEngine.Object.DestroyImmediate(data);}
            }
            if(state.nodes==null || state.segments==null || state.obstacles==null || state.nodes.Length!=sim.graph.nodes.Count || state.segments.Length!=sim.graph.segments.Count || state.obstacles.Length>5000)throw new FormatException("Estrutura de experiência incompleta ou excessiva.");
            var nodeIds=new HashSet<string>(sim.graph.nodes.Select(n=>n.id));var segmentIds=new HashSet<string>(sim.graph.segments.Select(s=>s.id));
            if(!nodeIds.Contains(state.originId??"") || !nodeIds.Contains(state.destinationId??"") || state.profileIndex<0 || state.profileIndex>=sim.profiles.Length || state.scenarioIndex<0 || state.scenarioIndex>=sim.scenarios.scenarios.Length)throw new FormatException("Origem, destino, perfil ou cenário inválido.");
            var seen=new HashSet<string>();
            foreach(var n in state.nodes)
            {
                if(n==null || !nodeIds.Contains(n.id??"") || !seen.Add(n.id) || !Range(n.height,-10000,10000) || !Range(n.baseHeight,-10000,10000) || !ElevationState(n.verification) || !ElevationState(n.baseVerification))throw new FormatException("Cota ou identificação de nó inválida.");
            }
            seen.Clear();
            foreach(var s in state.segments)
                if(s==null || !segmentIds.Contains(s.id??"") || !seen.Add(s.id) || !Range(s.totalWidth,.1f,100) || !Range(s.freeWidth,0,s.totalWidth) || !Range(s.crossSlope,-100,100) || !Range(s.roughness,0,1) || !Range(s.stepHeight,0,5) || !Range(s.amplitude,0,.2f) || !Range(s.period,.2f,100))throw new FormatException("Parâmetros de trecho inválidos.");
            foreach(var o in state.obstacles)
                if(o==null || !segmentIds.Contains(o.segmentId??"") || !Enum.IsDefined(typeof(ObstacleKind),o.kind) || !Range(o.position,0,1) || !Range(o.length,.05f,100) || !Range(o.width,.05f,100) || !Range(o.lateral,-100,100) || !Range(o.freeWidth,0,100) || !Range(o.height,0,10) || !Range(o.severity,0,1))throw new FormatException("Obstáculo inválido.");
            if(state.obstacles.GroupBy(o=>o.segmentId).Any(group=>group.Count()>32))throw new FormatException("Limite de 32 obstáculos por trecho excedido.");
        }
        private static bool ElevationState(VerificationState state)=>state==VerificationState.Simulated || state==VerificationState.ImportedUnverified;
        private static bool Range(float x,float min,float max)=>!float.IsNaN(x) && !float.IsInfinity(x) && x>=min && x<=max;
    }
}
