using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrajetoInclusivo.Editor
{
    public static class GeographicGraphExport
    {
        [Serializable] private sealed class Snapshot
        {
            public string attribution = "© OpenStreetMap contributors";
            public string license = "https://opendatacommons.org/licenses/odbl/1-0/";
            public string description = "Baseline experimental pedestrian graph. Offset sidewalks, connections, widths and elevations are simulated; accessibility is unassessed.";
            public GeographicReference reference;
            public Node[] nodes;
            public Edge[] edges;
        }
        [Serializable] private sealed class Node { public string id, name; public Vector3 localMetres; }
        [Serializable] private sealed class Edge
        {
            public string id, start, end, name, sourceFeatureId, geometryProvenance;
            public float width, freeWidth, crossSlopePercent;
            public bool hasAssessmentData, syntheticConnection, elevationIsSimulated;
        }
        [MenuItem("Trajeto Inclusivo/Exportar grafo geográfico para distribuição")]
        public static void ExportForDistribution()
        {ExportScene(GeographicSceneBuilder.ScenePath,"Builds/Windows/MapData");}
        public static void ExportFaesa()
        {ExportScene(GeographicSceneBuilder.FaesaScenePath,"Builds/Windows/MapData/FAESA");}
        public static void ExportScene(string scenePath,string folder)
        {
            Directory.CreateDirectory(folder);
            var preview = EditorSceneManager.OpenPreviewScene(scenePath);
            try
            {
                var sim = preview.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<SimulationController>()).Single();
                var snapshot = new Snapshot
                {
                    reference = sim.geographicContext.definition.reference,
                    nodes = sim.graph.nodes.Select(n=>new Node {id=n.id,name=n.displayName,localMetres=n.Position}).ToArray(),
                    edges = sim.graph.segments.Select(s=>new Edge {id=s.id,start=s.start.id,end=s.end.id,name=s.displayName,
                        sourceFeatureId=s.sourceFeatureId,geometryProvenance=s.geometryProvenance,width=s.totalWidth,freeWidth=s.freeWidth,
                        crossSlopePercent=s.crossSlopePercent,hasAssessmentData=s.hasAssessmentData,syntheticConnection=s.syntheticConnection,
                        elevationIsSimulated=s.elevationIsSimulated}).ToArray()
                };
                File.WriteAllText(folder+"/pedestrian_graph.json",JsonUtility.ToJson(snapshot,true));
                File.WriteAllText(folder+"/simulation_settings.json",JsonUtility.ToJson(sim.geographicContext.definition,true));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            foreach(var source in new[]{"Scripts/Geo/GeographicMapDefinition.cs","Editor/GeographicSceneBuilder.cs"})
                File.Copy(TestbedBuilder.Root+"/"+source,folder+"/"+Path.GetFileName(source),true);
            Debug.Log("OSM_GRAPH_EXPORT_OK: "+folder+"/pedestrian_graph.json");
        }
    }
}
