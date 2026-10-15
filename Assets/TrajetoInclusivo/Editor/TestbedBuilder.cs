using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrajetoInclusivo.Editor
{
    public static class TestbedBuilder
    {
        public const string Root = "Assets/TrajetoInclusivo";
        public const string ScenePath = Root + "/Scenes/AvenidaVitoria_Testbed.unity";

        [MenuItem("Trajeto Inclusivo/Criar ou reconstruir testbed sintético")]
        public static void CreateProject()
        {
            Directory.CreateDirectory(Root + "/Data/Profiles");
            Directory.CreateDirectory(Root + "/Data/Scenarios");
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Trajeto Inclusivo — TESTBED SINTÉTICO (1 unidade = 1 metro)");
            var graph = root.AddComponent<PedestrianGraph>();
            var map = root.AddComponent<MapGenerator>(); map.graph = graph;
            map.sidewalkMaterial = Material("Calcada", new Color(.62f, .65f, .64f));
            map.curbMaterial = Material("MeioFio", new Color(.86f, .87f, .8f));
            map.obstacleMaterial = Material("ObstaculoSimulado", new Color(1f, .36f, .12f));
            map.potholeMaterial = Material("BuracoSimulado", new Color(.12f, .08f, .065f));
            var asphalt = Material("Asfalto", new Color(.16f, .21f, .25f));
            var grass = Material("Solo", new Color(.22f, .34f, .30f));
            var building = Material("Edificacoes", new Color(.36f, .46f, .51f));
            var white = Material("Sinalizacao", new Color(.9f, .94f, .9f));
            var blue = Material("Nos", new Color(.23f, .68f, .93f));
            var urban = new GameObject("Geometria urbana provisória — sem georreferenciamento").transform;
            urban.SetParent(root.transform);
            Box("Base sintética", new Vector3(0, -.5f, 0), new Vector3(132, .5f, 92), grass, urban);
            Box("Via sintética norte", new Vector3(0, -.12f, 12), new Vector3(116, .2f, 19), asphalt, urban);
            Box("Via sintética sul", new Vector3(0, -.12f, -12), new Vector3(116, .2f, 19), asphalt, urban);
            for (int x = -44; x <= 44; x += 8)
            {
                Box("Linha viária", new Vector3(x, -.005f, 12), new Vector3(3, .02f, .12f), white, urban);
                Box("Linha viária", new Vector3(x, -.005f, -12), new Vector3(3, .02f, .12f), white, urban);
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -42 + i * 12;
                Box("Edificação simplificada " + i, new Vector3(x, 3 + i % 3, 36), new Vector3(9, 6 + 2 * (i % 3), 13), building, urban);
                Box("Edificação simplificada " + (i+8), new Vector3(x, 3, -36), new Vector3(9, 6, 13), building, urban);
            }
            var nodesRoot = new GameObject("Nós do grafo — clique nas esferas").transform; nodesRoot.SetParent(root.transform);
            var nodes = new Dictionary<string, PedestrianNode>();
            Node("A", "A • origem sintética", -50, 0, nodesRoot, nodes, graph, blue);
            Node("C", "Junção central oeste", -20, 0, nodesRoot, nodes, graph, blue);
            Node("M", "Topo da rampa artificial", 0, 0, nodesRoot, nodes, graph, blue);
            Node("E", "Junção central leste", 20, 0, nodesRoot, nodes, graph, blue);
            Node("B", "B • destino sintético", 50, 0, nodesRoot, nodes, graph, blue);
            foreach (var row in new[] { ("N", 24f), ("S", -24f) })
            {
                Node(row.Item1 + "0", "Junção " + row.Item1 + " oeste", -50, row.Item2, nodesRoot, nodes, graph, blue);
                Node(row.Item1 + "1", "Junção " + row.Item1 + " central oeste", -20, row.Item2, nodesRoot, nodes, graph, blue);
                Node(row.Item1 + "2", "Junção " + row.Item1 + " central leste", 20, row.Item2, nodesRoot, nodes, graph, blue);
                Node(row.Item1 + "3", "Junção " + row.Item1 + " leste", 50, row.Item2, nodesRoot, nodes, graph, blue);
            }
            var edgesRoot = new GameObject("Calçadas modulares e travessias").transform; edgesRoot.SetParent(root.transform);
            foreach (var pair in new[] { "A,C", "C,M", "M,E", "E,B", "A,N0", "N0,N1", "N1,N2", "N2,N3", "N3,B",
                         "A,S0", "S0,S1", "S1,S2", "S2,S3", "S3,B", "C,N1", "E,N2", "C,S1", "E,S2" })
            {
                var ids = pair.Split(',');
                var go = new GameObject(ids[0] + "-" + ids[1]); go.transform.SetParent(edgesRoot);
                var s = go.AddComponent<AccessibilitySegment>(); s.id = go.name;
                s.start = nodes[ids[0]]; s.end = nodes[ids[1]];
                bool crossing = Mathf.Abs(s.start.Position.z - s.end.Position.z) > 1;
                s.displayName = crossing ? "Travessia artificial em plataforma elevada" : "Calçada sintética";
                graph.segments.Add(s);
                if (crossing)
                {
                    for (int stripe = 0; stripe < 11; stripe++)
                    {
                        var p = Vector3.Lerp(s.start.Position, s.end.Position, (stripe + 1) / 12f);
                        Box("Faixa de travessia", p + Vector3.up * .035f, new Vector3(2.1f, .025f, .5f), white, urban);
                    }
                }
            }
            Label("REDE SINTÉTICA · DADOS NÃO VERIFICADOS", new Vector3(0, .1f, 43), 1.4f, urban);
            Label("CORREDOR NORTE · TESTE", new Vector3(0, .1f, 28), .9f, urban);
            Label("CORREDOR SUL · TESTE", new Vector3(0, .1f, -29), .9f, urban);
            var scenarios = root.AddComponent<ScenarioManager>(); scenarios.graph = graph; scenarios.map = map;
            scenarios.scenarios = MakeScenarios(); scenarios.currentIndex = 1;
            var sim = root.AddComponent<SimulationController>(); sim.graph = graph; sim.scenarios = scenarios;
            sim.profiles = MakeProfiles(); sim.origin = nodes["A"]; sim.destination = nodes["B"];
            var routes = new GameObject("Visualização das rotas").AddComponent<RouteVisualizer>(); routes.transform.SetParent(root.transform);
            routes.lineMaterial = Material("Rotas", Color.white, "Universal Render Pipeline/Particles/Unlit");
            sim.visualizer = routes;
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f, .12f, .17f); camera.nearClipPlane = .05f; camera.farClipPlane = 700;
            cameraObject.transform.SetPositionAndRotation(new Vector3(0, 93, -58), Quaternion.Euler(58, 0, 0));
            var cam = cameraObject.AddComponent<CameraController>(); cam.view = camera;
            var ui = root.AddComponent<SimulationUI>(); ui.simulation = sim; ui.cameraController = cam; cam.ui = ui;
            var inspection = root.AddComponent<RouteInspection>(); inspection.simulation = sim; inspection.cameraController = cam;
            ui.inspection = inspection;
            var marker = new GameObject("Marcador cinemático — cadeira esquemática"); marker.transform.SetParent(root.transform);
            Box("Assento", new Vector3(0, .5f, 0), new Vector3(.6f, .12f, .6f), blue, marker.transform);
            Box("Encosto", new Vector3(0, .8f, -.25f), new Vector3(.6f, .6f, .1f), blue, marker.transform);
            for (int i = -1; i <= 1; i += 2)
            {
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder); wheel.name = "Roda ilustrativa";
                wheel.transform.SetParent(marker.transform); wheel.transform.localPosition = new Vector3(i * .37f, .32f, -.05f);
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90); wheel.transform.localScale = new Vector3(.62f, .045f, .62f);
                wheel.GetComponent<Renderer>().sharedMaterial = asphalt;
                UnityEngine.Object.DestroyImmediate(wheel.GetComponent<Collider>());
            }
            inspection.marker = marker.transform;
            PrefabUtility.SaveAsPrefabAsset(marker, Root + "/Prefabs/WheelchairMarker.prefab");
            marker.SetActive(false);
            var light = new GameObject("Sol").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.6f; light.transform.rotation = Quaternion.Euler(48, -32, 0); light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.65f, .7f, .77f);
            map.RefreshGeometry();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh.name.StartsWith("Trecho ", StringComparison.Ordinal)) continue;
                string path = Root + "/Meshes/" + filter.transform.parent.name + "_" + filter.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                filter.sharedMesh.SetPreBakeCollisionMesh(false, true);
                if (existing == null) AssetDatabase.CreateAsset(filter.sharedMesh, path);
                else { EditorUtility.CopySerialized(filter.sharedMesh, existing); filter.sharedMesh = existing; filter.GetComponent<MeshCollider>().sharedMesh = existing; }
            }
            SaveReusablePrefabs(map, graph);
            graph.Rebuild();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Trajeto Inclusivo"; PlayerSettings.productName = "Trajeto Inclusivo — Testbed Sintético";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 1000;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("TRAJETO_SETUP_OK: " + ScenePath);
        }
        private static void Node(string id, string displayName, float x, float z, Transform parent,
            Dictionary<string, PedestrianNode> nodes, PedestrianGraph graph, Material material)
        {
            var go = new GameObject(id); go.transform.SetParent(parent); go.transform.position = new Vector3(x, .18f, z);
            var node = go.AddComponent<PedestrianNode>(); node.id = id; node.displayName = displayName; nodes.Add(id, node); graph.nodes.Add(node);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = "Selecionar nó " + id;
            sphere.transform.SetParent(go.transform, false); sphere.transform.localPosition = Vector3.up * .6f;
            sphere.transform.localScale = Vector3.one * 1.3f; sphere.GetComponent<Renderer>().sharedMaterial = material;
            Label(id, go.transform.position + new Vector3(0, 1.4f, 1.5f), .9f, go.transform);
        }
        private static void Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
        private static void Label(string text, Vector3 p, float size, Transform parent)
        {
            var go = new GameObject(text); go.transform.SetParent(parent); go.transform.position = p;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var label = go.AddComponent<TextMesh>(); label.text = text; label.characterSize = size / 10;
            label.fontSize = 80; label.anchor = TextAnchor.MiddleCenter; label.color = new Color(.75f, .94f, .91f);
        }
        private static Material Material(string name, Color color, string shader = "Universal Render Pipeline/Lit")
        {
            var path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find(shader)) { name = name }; AssetDatabase.CreateAsset(mat, path); }
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .15f);
            EditorUtility.SetDirty(mat); return mat;
        }
        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            EditorUtility.SetDirty(asset); return asset;
        }
        private static AccessibilityProfile[] MakeProfiles()
        {
            var walk = Asset<AccessibilityProfile>(Root + "/Data/Profiles/Pedestre.asset"); walk.displayName = "Pedestre sem restrição simulada";
            walk.requiredWidth = .6f; walk.maxStepHeight = .22f; walk.maxUphillPercent = 25; walk.maxDownhillPercent = 30;
            walk.maxCrossSlopePercent = 12; walk.maxRoughness = .95f;
            walk.weights.uphillPerPercent = .025f; walk.weights.downhillPerPercent = .02f;
            var manual = Asset<AccessibilityProfile>(Root + "/Data/Profiles/CadeiraManual.asset");
            manual.displayName = "Cadeira de rodas manual";
            var motor = Asset<AccessibilityProfile>(Root + "/Data/Profiles/CadeiraMotorizada.asset"); motor.displayName = "Cadeira de rodas motorizada";
            motor.requiredWidth = .9f; motor.maxStepHeight = .04f; motor.maxUphillPercent = 10; motor.maxDownhillPercent = 12;
            motor.maxCrossSlopePercent = 4; motor.maxRoughness = .7f;
            return new[] { walk, manual, motor };
        }
        private static ScenarioDefinition[] MakeScenarios()
        {
            string[] names = { "1 · Sem obstáculos", "2 · Degrau", "3 · Inclinação de 12%", "4 · Buraco", "5 · Obras" };
            string[] descriptions = {
                "Sem obstáculos. Rampa suave de 3% no corredor central; ambas as rotas disponíveis.",
                "Degrau artificial de 16 cm no caminho mais curto. Cadeiras de rodas devem contornar.",
                "Topo central elevado em 2,40 m: subida e descida de 12%. Alternativas laterais planas.",
                "Buraco simulado deixa apenas 0,65 m livres. Procure uma alternativa para a cadeira de rodas.",
                "Obras fictícias interditam o trecho central para todos os perfis. O desvio usa travessias elevadas sintéticas."
            };
            var result = new ScenarioDefinition[5];
            for (int i = 0; i < 5; i++)
            {
                var s = Asset<ScenarioDefinition>(Root + "/Data/Scenarios/Scenario" + (i+1) + ".asset");
                s.displayName = names[i]; s.description = descriptions[i]; s.elevationOffset = i == 2 ? 2.4f : .6f;
                s.obstacles = i == 1 ? new[] { new ObstacleSpecification { kind = ObstacleKind.Step, heightOrDepth = .16f } }
                    : i == 3 ? new[] { new ObstacleSpecification { kind = ObstacleKind.Pothole, remainingFreeWidth = .65f, length = 1.6f, heightOrDepth = .12f, severity = .45f } }
                    : i == 4 ? new[] { new ObstacleSpecification { kind = ObstacleKind.Construction, remainingFreeWidth = 0, length = 2, heightOrDepth = 1.1f, canBypass = false, blocksPassage = true } }
                    : Array.Empty<ObstacleSpecification>();
                result[i] = s;
            }
            return result;
        }
        private static void SaveReusablePrefabs(MapGenerator map, PedestrianGraph graph)
        {
            // A disconnected module carries its geometry; endpoint links must be assigned after placement.
            var template = UnityEngine.Object.Instantiate(graph.segments[0].gameObject);
            var segment = template.GetComponent<AccessibilitySegment>(); segment.start = null; segment.end = null;
            PrefabUtility.SaveAsPrefabAsset(template, Root + "/Prefabs/SidewalkModule.prefab");
            UnityEngine.Object.DestroyImmediate(template);
            var obstacle = new GameObject("ObstacleModule_SIMULATED"); obstacle.AddComponent<AccessibilityObstacle>();
            PrefabUtility.SaveAsPrefabAsset(obstacle, Root + "/Prefabs/ObstacleModule.prefab");
            UnityEngine.Object.DestroyImmediate(obstacle);
        }
    }
}
