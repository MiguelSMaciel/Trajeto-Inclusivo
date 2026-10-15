using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrajetoInclusivo.Editor
{
    public static class GeographicSceneBuilder
    {
        public const string ScenePath = TestbedBuilder.Root + "/Scenes/AvenidaVitoria_Geografico.unity";
        public const string FaesaScenePath = TestbedBuilder.Root + "/Scenes/AvenidaVitoria_FAESA.unity";
        private static string Data = TestbedBuilder.Root + "/Data/Geography";
        private static string Meshes = TestbedBuilder.Root + "/Meshes/Geographic";
        [Serializable] private sealed class Metadata { public Origin origin; public string downloadedAtUtc; }
        [Serializable] private sealed class Origin { public double longitude, latitude; public string osmNodeId; }

        [MenuItem("Trajeto Inclusivo/Gerar cena geográfica do recorte OSM local")]
        public static void BuildFromLocalData()
        {Build(false);}
        [MenuItem("Trajeto Inclusivo/Gerar cena ampliada até a FAESA")]
        public static void BuildFaesa()
        {Build(true);}
        private static void Build(bool faesa)
        {
            Data=TestbedBuilder.Root+"/Data/Geography"+(faesa?"/FAESA":"");
            Meshes=TestbedBuilder.Root+"/Meshes/"+(faesa?"FAESA":"Geographic");
            string scenePath=faesa?FaesaScenePath:ScenePath;
            if (!File.Exists(TestbedBuilder.ScenePath)) TestbedBuilder.CreateProject();
            Directory.CreateDirectory(Meshes); Directory.CreateDirectory(Data + "/Scenarios");
            AssetDatabase.Refresh();
            var definition = Asset<GeographicMapDefinition>(Data + "/AvenidaVitoria.asset");
            definition.geoJson = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "/avenida_vitoria.geojson.json");
            definition.provenance = AssetDatabase.LoadAssetAtPath<TextAsset>(Data + "/provenance.json");
            if (definition.geoJson == null || definition.provenance == null) throw new InvalidOperationException("Execute Tools/Export-OsmGeoJson.ps1 antes de gerar a cena.");
            var metadata = JsonUtility.FromJson<Metadata>(definition.provenance.text);
            definition.reference.coordinates = GeoCoordinateSystem.Wgs84LongitudeLatitude;
            definition.reference.originLongitude = metadata.origin.longitude; definition.reference.originLatitude = metadata.origin.latitude;
            definition.faesaReferenceId=faesa?"way/51374617":"";
            definition.datasetDate=metadata.downloadedAtUtc.Substring(0,10);
            var features = GeoDataImporter.Import(definition.geoJson.text, definition.reference);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Avenida Vitória — eixos OSM / circulação experimental");
            var context = root.AddComponent<GeographicDatasetContext>(); context.definition = definition;
            var graph = root.AddComponent<PedestrianGraph>();
            var map = root.AddComponent<MapGenerator>(); map.graph = graph;
            map.generateConnectionSurfaces = false;
            map.sidewalkMaterial = Material("Calcada"); map.curbMaterial = Material("MeioFio");
            map.obstacleMaterial = Material("ObstaculoSimulado"); map.potholeMaterial = Material("BuracoSimulado");
            var roads = new GeographicMeshBuilder(); var buildings = new GeographicMeshBuilder();
            var plaza = new GeographicMeshBuilder(); var campus = new GeographicMeshBuilder(); var paths = new GeographicMeshBuilder();
            bool first = true;
            foreach (var feature in features)
            {
                foreach (var part in feature.parts) foreach (var p in part)
                { if (first) { context.mapBounds = new Bounds(p, Vector3.zero); first = false; } else context.mapBounds.Encapsulate(p); }
                string kind = feature.Property("kind");
                if (kind == "road")
                {
                    context.roadFeatureCount++;
                    string highway = feature.Property("highway");
                    float width = highway == "footway" || highway == "steps" || highway == "path" ? 1.8f : GeographicNetworkBuilder.RoadWidth(feature, definition);
                    foreach (var part in feature.parts)
                        for (int i = 0; i < part.Length-1; i++)
                        { if (highway == "steps" || highway == "footway" || highway == "path") paths.Road(part[i], part[i+1], width, .04f); else roads.Road(part[i], part[i+1], width); }
                }
                else if (kind == "building")
                {
                    float height = definition.assumedBuildingHeight;
                    if (float.TryParse(feature.Property("height"), NumberStyles.Float, CultureInfo.InvariantCulture, out var h) && h > 0) height = h;
                    else if (float.TryParse(feature.Property("building:levels"), NumberStyles.Float, CultureInfo.InvariantCulture, out var levels) && levels > 0) height = levels*3;
                    if (buildings.Polygon(feature.parts[0], .02f, Mathf.Clamp(height, 2, 50))) context.buildingFeatureCount++;
                    else context.omittedPolygonCount++;
                }
                else if (kind == "park") { if (!plaza.Polygon(feature.parts[0], .025f, .025f)) context.omittedPolygonCount++; }
                else if (kind == "campus") { if (!campus.Polygon(feature.parts[0], .01f, .01f)) context.omittedPolygonCount++; }
            }
            var urban = new GameObject("Geometrias OSM combinadas — alturas/larguras não levantadas").transform; urban.SetParent(root.transform);
            SaveMeshObject("Eixos viários OSM — largura aproximada", roads, Material("Asfalto"), urban);
            SaveMeshObject("Edificações OSM — altura aproximada", buildings, Material("Edificacoes"), urban);
            SaveMeshObject("Praça Asdrúbal Soares — polígono OSM", plaza, Material("Solo"), urban);
            SaveMeshObject("Terreno do Ifes — polígono OSM", campus, Material("TerrenoCampus", new Color(.23f, .35f, .4f)), urban);
            SaveMeshObject("Caminhos e escadarias OSM — sem cotas", paths, Material("CaminhosOSM", new Color(.5f, .43f, .38f)), urban);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "Plano de referência artificial — sem DEM";
            ground.transform.SetParent(root.transform); ground.transform.position = context.mapBounds.center + Vector3.down*.4f;
            ground.transform.localScale = new Vector3(context.mapBounds.size.x+50, .5f, context.mapBounds.size.z+50);
            ground.GetComponent<Renderer>().sharedMaterial = Material("Solo");
            GeographicNetworkBuilder.Build(features, definition, graph, Material("Nos"), out var origin, out var destination);
            context.plaza=origin;context.ifes=destination;
            if(faesa){context.faesa=graph.nodes.Single(n=>n.id=="C");destination=context.faesa;}
            graph.Rebuild();
            var profile = AssetDatabase.LoadAssetAtPath<AccessibilityProfile>(TestbedBuilder.Root + "/Data/Profiles/CadeiraManual.asset");
            var route = RouteCalculator.Calculate(graph, origin, destination, profile, false);
            if (!route.Found) throw new InvalidOperationException("Recorte não contém conexão entre praça e portão do Ifes na rede aproximada.");
            var hotspot = FindDetourEdge(graph, route, origin, destination, profile);
            var mid = new GameObject("M — elevação ARTIFICIAL de teste").AddComponent<PedestrianNode>();
            mid.transform.SetParent(root.transform); mid.transform.position = (hotspot.start.Position + hotspot.end.Position)*.5f;
            mid.id = "GeoRamp"; mid.displayName = "Ponto de elevação artificial sobre " + hotspot.displayName; graph.nodes.Add(mid);
            var continuation = new GameObject("Continuação do trecho de teste").AddComponent<AccessibilitySegment>();
            continuation.transform.SetParent(hotspot.transform.parent);
            continuation.start = mid; continuation.end = hotspot.end; continuation.id = hotspot.id + ":test-split";
            continuation.displayName = hotspot.displayName; continuation.sourceFeatureId = hotspot.sourceFeatureId;
            continuation.geometryProvenance = hotspot.geometryProvenance; continuation.hasAssessmentData = false;
            continuation.totalWidth = continuation.freeWidth = definition.simulatedSidewalkWidth; continuation.crossSlopePercent = 0; continuation.roughness = 0;
            graph.segments.Add(continuation); hotspot.end = mid;
            var scenarios = root.AddComponent<ScenarioManager>(); scenarios.graph = graph; scenarios.map = map;
            scenarios.scenarios = MakeScenarios(hotspot, mid, definition.simulatedSidewalkWidth); scenarios.currentIndex = 0;
            var sim = root.AddComponent<SimulationController>(); sim.graph = graph; sim.scenarios = scenarios;
            sim.geographicContext = context; sim.origin = origin; sim.destination = destination;
            sim.profiles = new[] { "Pedestre", "CadeiraManual", "CadeiraMotorizada" }.Select(n => AssetDatabase.LoadAssetAtPath<AccessibilityProfile>(TestbedBuilder.Root + "/Data/Profiles/"+n+".asset")).ToArray();
            var visualizer = new GameObject("Rotas").AddComponent<RouteVisualizer>(); visualizer.transform.SetParent(root.transform);
            visualizer.lineMaterial = Material("Rotas"); sim.visualizer = visualizer;
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.backgroundColor = new Color(.075f, .12f, .17f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = .1f; camera.farClipPlane = 5000;
            var cameraController = camera.gameObject.AddComponent<CameraController>(); cameraController.view = camera;
            cameraController.frameCenter = context.mapBounds.center;
            cameraController.frameSize = new Vector2(context.mapBounds.size.x*1.12f, context.mapBounds.size.z*1.12f);
            var ui = root.AddComponent<SimulationUI>(); ui.simulation = sim; ui.cameraController = cameraController; cameraController.ui = ui;
            var inspection = root.AddComponent<RouteInspection>(); inspection.simulation = sim; inspection.cameraController = cameraController; ui.inspection = inspection;
            var marker = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TestbedBuilder.Root + "/Prefabs/WheelchairMarker.prefab"));
            marker.transform.SetParent(root.transform); marker.SetActive(false); inspection.marker = marker.transform;
            var sun = new GameObject("Luz").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.6f;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0); sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.65f, .7f, .77f);
            var plazaFeature = features.First(f => f.id == definition.plazaId);
            Label("Praça Asdrúbal Soares", GeographicNetworkBuilder.PolygonCentroid(plazaFeature.parts[0])+Vector3.up*3, 5, root.transform);
            Label("Ifes · Campus Vitória", GeographicNetworkBuilder.PolygonCentroid(features.First(f => f.id == definition.campusId).parts[0])+Vector3.up*12, 6, root.transform);
            if(faesa)Label("FAESA · Av. Vitória 2220\nReferência aproximada na calçada",context.faesa.Position+Vector3.up*4,4,root.transform);
            foreach (var group in features.Where(f => f.Property("kind") == "road" && f.name.Length > 0).GroupBy(f => f.name))
            {
                var longest = group.OrderByDescending(f => Vector3.Distance(f.parts[0][0], f.parts[0][1])).First();
                var p = (longest.parts[0][0] + longest.parts[0][1])*.5f + Vector3.up*.2f;
                Label(group.Key, p, group.Key == "Avenida Vitória" ? 5 : 3, root.transform);
            }
            map.RefreshGeometry();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if (AssetDatabase.Contains(filter.sharedMesh) || !filter.sharedMesh.name.StartsWith("Trecho ", StringComparison.Ordinal)) continue;
                    string meshName = filter.transform.parent.name + "_" + filter.name;
                    var mesh = SaveMesh(meshName, filter.sharedMesh); filter.sharedMesh = mesh; filter.GetComponent<MeshCollider>().sharedMesh = mesh;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            cameraController.FrameMap(); camera.transform.SetPositionAndRotation(cameraController.target - Quaternion.Euler(58,0,0)*Vector3.forward*cameraController.distance, Quaternion.Euler(58,0,0));
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = (File.Exists(FaesaScenePath)?new[]{FaesaScenePath,ScenePath,TestbedBuilder.ScenePath}:new[]{ScenePath,TestbedBuilder.ScenePath}).Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
            EditorUtility.SetDirty(definition); AssetDatabase.SaveAssets();
            var summary = $"OSM_SETUP_OK: {graph.nodes.Count} nós; {graph.segments.Count} trechos; {context.roadFeatureCount} eixos; {context.buildingFeatureCount} edifícios; {context.omittedPolygonCount} polígonos rejeitados. Rota geométrica inicial: {route.distance:F2} m. Trecho artificial: {hotspot.id}";
            File.WriteAllText(faesa?"TestResults/FaesaSetup.txt":"TestResults/GeographicSetup.txt", summary); Debug.Log(summary);
        }
        private static AccessibilitySegment FindDetourEdge(PedestrianGraph graph, RouteResult route, PedestrianNode a, PedestrianNode b, AccessibilityProfile profile)
        {
            foreach (var candidate in route.arcs.Select(arc => arc.segment).Where(s => !s.syntheticConnection && s.Length >= 8).OrderByDescending(s => s.Length))
            {
                candidate.networkOpen = false;
                var alternative = RouteCalculator.Calculate(graph, a, b, profile, false);
                candidate.networkOpen = true;
                if (alternative.Found && alternative.distance > route.distance + 1) return candidate;
            }
            throw new InvalidOperationException("Não foi encontrado trecho de teste com desvio conectado. Revise a topologia aproximada.");
        }
        private static ScenarioDefinition[] MakeScenarios(AccessibilitySegment target, PedestrianNode raised, float width)
        {
            var result = new ScenarioDefinition[5];
            string[] names = { "1 · Sem obstáculos", "2 · Degrau simulado", "3 · Inclinação de 12% simulada", "4 · Buraco simulado", "5 · Obras simuladas" };
            for (int i = 0; i < 5; i++)
            {
                var s = Asset<ScenarioDefinition>(Data + "/Scenarios/GeoScenario" + (i+1) + ".asset");
                s.displayName = names[i]; s.raisedNodeId = raised.id; s.elevationOffset = i == 2 ? target.HorizontalDistance*.12f : 0;
                s.description = "Condição ARTIFICIAL sobre " + target.displayName + ". Não representa condições observadas no local.";
                s.obstacles = i == 1 ? new[] {new ObstacleSpecification {segmentId=target.id, kind=ObstacleKind.Step, heightOrDepth=.16f, remainingFreeWidth=width}}
                    : i == 3 ? new[] {new ObstacleSpecification {segmentId=target.id, kind=ObstacleKind.Pothole, heightOrDepth=.12f, remainingFreeWidth=.65f, severity=.45f}}
                    : i == 4 ? new[] {new ObstacleSpecification {segmentId=target.id, kind=ObstacleKind.Construction, remainingFreeWidth=0, blocksPassage=true, canBypass=false, heightOrDepth=1.1f}}
                    : Array.Empty<ObstacleSpecification>(); result[i] = s;
            }
            return result;
        }
        private static T Asset<T>(string path) where T:ScriptableObject
        { var result = AssetDatabase.LoadAssetAtPath<T>(path); if (result == null) { result = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(result,path); } EditorUtility.SetDirty(result); return result; }
        private static Material Material(string name, Color? color = null)
        {
            var path = TestbedBuilder.Root + "/Materials/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = color ?? Color.gray; AssetDatabase.CreateAsset(m,path); }
            return m;
        }
        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = Meshes + "/" + name.Replace('/','_').Replace(':','_') + ".asset";
            mesh.name = Path.GetFileNameWithoutExtension(path);
            mesh.SetPreBakeCollisionMesh(false, true);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,existing); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(mesh); return existing;
        }
        private static void SaveMeshObject(string name, GeographicMeshBuilder builder, Material material, Transform root)
        {
            if (builder.VertexCount == 0) return;
            var go = new GameObject(name); go.transform.SetParent(root,false);
            var mesh = SaveMesh(name,builder.ToMesh(name)); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material; go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        private static void Label(string text, Vector3 position, float height, Transform root)
        {
            var go = new GameObject(text); go.transform.SetParent(root); go.transform.position = position; go.transform.rotation = Quaternion.Euler(90,0,0);
            var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = 80; label.characterSize = height/8;
            label.anchor = TextAnchor.MiddleCenter; label.color = new Color(.86f,.97f,.9f);
        }
    }
}
