using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrajetoInclusivo
{
    // Opt-in checks in the shipped player, independent from Editor test assemblies.
    public sealed class InterfaceSmokeCheck:MonoBehaviour
    {
        private int errors;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stage4-smoke")<0)return;
            Application.runInBackground=true;var go=new GameObject("Interface validation");DontDestroyOnLoad(go);go.AddComponent<InterfaceSmokeCheck>();
        }
        private void OnEnable()=>Application.logMessageReceived+=OnLog;
        private void OnDisable()=>Application.logMessageReceived-=OnLog;
        private void OnLog(string message,string trace,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors++;}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-testbed-results");
            string output=index>=0 && index+1<args.Length?args[index+1]:Application.persistentDataPath;Directory.CreateDirectory(output);
            var report=new StringBuilder();var csv=new StringBuilder("scene,scenario,profile,conventional_m,accessible_m,blocked_on_conventional,pass\n");bool passed=true;
            // Recreate the framebuffer even when the previous saved resolution matches the target.
            Screen.SetResolution(1024,768,FullScreenMode.Windowed);
            for(int i=0;i<4;i++)yield return null;
            Screen.SetResolution(1600,1000,FullScreenMode.Windowed);
            for(int i=0;i<4;i++)yield return null;
            var scenes=Application.CanStreamedLevelBeLoaded("AvenidaVitoria_FAESA")?new[]{"AvenidaVitoria_FAESA","AvenidaVitoria_Geografico","AvenidaVitoria_Testbed"}:new[]{"AvenidaVitoria_Geografico","AvenidaVitoria_Testbed"};
            foreach(string scene in scenes)
            {
                if(SceneManager.GetActiveScene().name!=scene){yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;}
                var sim=FindAnyObjectByType<SimulationController>();var ui=FindAnyObjectByType<SimulationUI>();var camera=FindAnyObjectByType<CameraController>();
                bool pacing=Application.targetFrameRate==DesktopRuntimeDefaults.TargetFrameRate && QualitySettings.vSyncCount==0;
                passed &= pacing;
                report.AppendLine($"{scene} frame limit={Application.targetFrameRate}, vSync={QualitySettings.vSyncCount}: {pacing}");
                float elapsed=0;
                for(int sample=0;sample<30;sample++){yield return null;elapsed+=Time.unscaledDeltaTime;}
                float sampledFps=elapsed>0?30/elapsed:0;
                bool paced=sampledFps>0 && sampledFps<=DesktopRuntimeDefaults.TargetFrameRate*1.2f;
                passed &= paced;report.AppendLine($"{scene} sampled FPS over 30 frames={sampledFps:F1}, within configured ceiling with 20% timing tolerance: {paced}");
                if(sim.geographicContext!=null)
                {
                    bool unknown=sim.accessible.failure==RouteFailure.InsufficientData;passed &= unknown;report.AppendLine(scene+" initial unknown: "+unknown);
                    camera.FrameRoute(sim.conventional);yield return null;yield return new WaitForEndOfFrame();
                    CaptureFrame(output,"Stage4-NaoAvaliado-"+scene,report);
                    sim.geographicContext.SetAssessmentMode(true,sim);
                }
                float baseline=0;
                for(int scenario=0;scenario<5;scenario++)for(int profile=0;profile<3;profile++)
                {
                    sim.scenarios.Apply(scenario);sim.SetProfile(profile);yield return null;yield return null;
                    if(scenario==0 && profile==0)baseline=sim.conventional.distance;
                    bool ok=sim.conventional.Found && sim.accessible.Found && sim.accessible.arcs.All(a=>AccessibilityEvaluator.Evaluate(a.segment,sim.Profile,a.forward).Passable)
                        && !sim.conventional.arcs.Any(a=>a.segment.HasClosure) && sim.comparison.accessible.unknownCount==0 && sim.comparison.accessible.blockedCount==0;
                    if(scenario>=1 && scenario<=3 && profile>0)ok &= sim.accessible.distance>baseline+1;
                    if(scenario==4)ok &= sim.conventional.distance>baseline+1;
                    passed &= ok;
                    csv.AppendLine(string.Join(",",scene,scenario+1,profile,sim.conventional.distance.ToString("F3",CultureInfo.InvariantCulture),sim.accessible.distance.ToString("F3",CultureInfo.InvariantCulture),sim.comparison.conventional.blockedCount,ok?"PASS":"FAIL"));
                }
                if(sim.geographicContext!=null && sim.geographicContext.faesa!=null)
                {
                    for(int scenario=0;scenario<5;scenario++)for(int profile=0;profile<3;profile++)
                    {
                        ui.SetFaesaRoute(true);sim.scenarios.Apply(scenario);sim.SetProfile(profile);yield return null;yield return null;
                        bool ok=sim.conventional.Found && sim.accessible.Found && sim.accessible.arcs.All(a=>AccessibilityEvaluator.Evaluate(a.segment,sim.Profile,a.forward).Passable);
                        passed &= ok;csv.AppendLine(string.Join(",",scene+"-Ifes",scenario+1,profile,sim.conventional.distance.ToString("F3",CultureInfo.InvariantCulture),sim.accessible.distance.ToString("F3",CultureInfo.InvariantCulture),sim.comparison.conventional.blockedCount,ok?"PASS":"FAIL"));
                    }
                    ui.SetFaesaRoute(false);
                }
                sim.scenarios.Apply(1);sim.SetProfile(1);yield return null;yield return null;
                if(sim.geographicContext!=null && sim.geographicContext.faesa!=null)
                {
                    var origin=sim.origin;var destination=sim.destination;
                    ui.FocusLandmark(sim.geographicContext.faesa);yield return null;yield return new WaitForEndOfFrame();
                    bool focus=Vector3.Distance(camera.target,sim.geographicContext.faesa.Position)<.001f && sim.origin==origin && sim.destination==destination;
                    passed &= focus;report.AppendLine("FAESA landmark focus without changing endpoints: "+focus);
                    CaptureFrame(output,"Navigation-FAESA-Closeup",report);
                }
                camera.FrameRoute(sim.accessible);ui.ShowProfileComparison(false);yield return null;yield return new WaitForEndOfFrame();
                CaptureFrame(output,"Stage4-Comparacao-"+scene,report);
                ui.ShowProfileComparison(true);yield return null;yield return new WaitForEndOfFrame();CaptureFrame(output,"Stage4-Perfis-"+scene,report);
                ui.ShowProfileComparison(false);
                var issue=sim.comparison.conventional.segments.First(s=>s.rating==AccessibilityRating.Inaccessible).segment;
                ui.InspectSegment(issue);yield return null;yield return new WaitForEndOfFrame();CaptureFrame(output,"Stage4-Inspecao-"+scene,report);
                if(scene=="AvenidaVitoria_Testbed")
                {
                    var playback=FindAnyObjectByType<RouteInspection>();
                    var originalRoute=sim.accessible;float baseSpeed=playback.speedMetresPerSecond,timeScale=Time.timeScale;
                    playback.speedMetresPerSecond=.1f;playback.Begin();
                    Vector3 start=playback.marker.position;float expected=0;bool speedOk=true;
                    float[] factors={.5f,1f,1.5f,2f,4f,8f};
                    for(int speed=0;speed<factors.Length;speed++)
                    {
                        var before=playback.marker.position;playback.SetPlaybackSpeedIndex(speed);
                        speedOk &= playback.marker.position==before;
                        for(int frame=0;frame<3;frame++){yield return null;expected+=.1f*factors[speed]*Time.deltaTime;}
                        speedOk &= Mathf.Abs(Vector3.Distance(start,playback.marker.position)-expected)<.002f && playback.Running;
                    }
                    speedOk &= sim.accessible==originalRoute && Time.timeScale==timeScale;
                    playback.Stop();speedOk &= playback.PlaybackSpeedMultiplier==8;
                    playback.speedMetresPerSecond=baseSpeed;playback.Begin();sim.selectedSegment=null;
                    yield return null;yield return new WaitForEndOfFrame();CaptureFrame(output,"Playback-8x",report);
                    playback.Stop();playback.SetPlaybackSpeedIndex(1);
                    passed &= speedOk;report.AppendLine("Playback 0.5x / 1x / 1.5x / 2x / 4x / 8x, continuous position, same route and global time: "+speedOk);
                }
                sim.scenarios.Apply(2);yield return null;yield return null;
                var nodeOrigin=sim.origin;var nodeDestination=sim.destination;var nodeRoute=sim.conventional;var nodeWorld=sim.worldEditor.CaptureJson();
                var scenarioDefinition=sim.scenarios.scenarios[2];var elevated=sim.graph.nodes.Find(n=>n.id==scenarioDefinition.raisedNodeId);
                Physics.SyncTransforms();bool nodeInspection=ui.HandleMapRay(new Ray(elevated.Position+Vector3.up*5,Vector3.down));
                nodeInspection &= ui.SelectedNode==elevated && sim.origin==nodeOrigin && sim.destination==nodeDestination && sim.conventional==nodeRoute && sim.worldEditor.CaptureJson()==nodeWorld;
                ui.InspectScenarioChange();yield return null;yield return new WaitForEndOfFrame();
                nodeInspection &= ui.SelectedNode==elevated && Vector3.Distance(camera.target,elevated.Position)<.001f;
                CaptureFrame(output,"Nodes-Inspecao-"+scene,report);
                if(sim.geographicContext!=null && sim.geographicContext.faesa!=null)
                {ui.InspectNode(sim.geographicContext.faesa,true);yield return null;yield return new WaitForEndOfFrame();CaptureFrame(output,"Nodes-FAESA",report);}
                ui.UseInspectedNodeAsEndpoint(true);ui.UseInspectedNodeAsEndpoint(false);yield return null;yield return null;
                nodeInspection &= sim.accessible.Found && sim.accessible.distance==0;
                ui.CloseInspection();nodeInspection &= ui.SelectedNode==null;
                sim.origin=nodeOrigin;sim.destination=nodeDestination;sim.scenarios.Apply(1);yield return null;yield return null;
                ui.InspectScenarioChange();nodeInspection &= sim.selectedSegment!=null && ui.SelectedNode==null && scenarioDefinition.raisedNodeId==elevated.id;
                passed &= nodeInspection;report.AppendLine(scene+" node raycast, unchanged world, scenario locator, endpoints and closing: "+nodeInspection);
                var endpoint=sim.origin;ui.BeginPicking(false);Physics.SyncTransforms();
                bool picking=ui.HandleMapRay(new Ray(endpoint.Position+Vector3.up*5,Vector3.down));yield return null;yield return null;
                picking &= sim.destination==endpoint && sim.accessible.Found && sim.accessible.distance==0;passed &= picking;report.AppendLine(scene+" endpoint picking / zero-length route: "+picking);
                if(scene=="AvenidaVitoria_Testbed")
                {
                    Screen.SetResolution(1024,768,FullScreenMode.Windowed);ui.ShowProfileComparison(false);yield return null;yield return null;
                    yield return new WaitForEndOfFrame();CaptureFrame(output,"Stage4-1024x768",report);
                    ui.InspectNode(elevated,true);yield return null;yield return new WaitForEndOfFrame();CaptureFrame(output,"Nodes-1024x768",report);
                }
            }
            yield return SceneManager.LoadSceneAsync(scenes[0]);yield return null;yield return null;
            var windowUi=FindAnyObjectByType<SimulationUI>();var windowSim=FindAnyObjectByType<SimulationController>();var windowCamera=FindAnyObjectByType<CameraController>();
            var unchangedRoute=windowSim.conventional;string unchangedWorld=windowSim.worldEditor.CaptureJson();
            int firstWidth=Screen.width,firstHeight=Screen.height;bool resized=false;
            foreach(var size in new[]{new Vector2Int(1600,1000),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(1024,768)})
            {
                Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);
                for(int frame=0;frame<5;frame++)yield return null;
                windowCamera.FrameRoute(windowSim.conventional);yield return null;yield return new WaitForEndOfFrame();
                bool layoutOk=CheckWindowLayout(windowUi,windowCamera) && unchangedRoute==windowSim.conventional && unchangedWorld==windowSim.worldEditor.CaptureJson();
                resized |= Screen.width!=firstWidth || Screen.height!=firstHeight;
                passed &= layoutOk;report.AppendLine($"Window layout requested={size.x}x{size.y} actual={Screen.width}x{Screen.height}, cropped viewport, aligned labels, clear top margin, unchanged world: {layoutOk}");
                CaptureFrame(output,$"WindowLayout-{Screen.width}x{Screen.height}",report);
            }
            passed &= resized;report.AppendLine("Player window changed size: "+resized);
            passed &= errors==0;report.AppendLine("Runtime errors: "+errors);report.AppendLine(passed?"TRAJETO_STAGE4_OK":"TRAJETO_STAGE4_FAILED");
            File.WriteAllText(Path.Combine(output,"Stage4-Player-Matrix.csv"),csv.ToString());File.WriteAllText(Path.Combine(output,"Stage4-Player.txt"),report.ToString());
            Debug.Log(passed?"TRAJETO_STAGE4_OK":"TRAJETO_STAGE4_FAILED");Application.Quit(passed?0:1);
        }
        private static bool CheckWindowLayout(SimulationUI ui,CameraController camera)
        {
            var layout=new SimulationLayout(Screen.width,Screen.height);var pixels=camera.view.pixelRect;
            bool valid=Mathf.Abs(pixels.xMin-layout.map.xMin*layout.scale)<1 && Mathf.Abs(pixels.xMax-layout.map.xMax*layout.scale)<1
                && Mathf.Abs(pixels.yMin-(Screen.height-layout.map.yMax*layout.scale))<1 && Mathf.Abs(pixels.yMax-(Screen.height-layout.map.yMin*layout.scale))<1;
            foreach(var marker in ui.GetLandmarkMarkers())
            {
                var screen=camera.view.WorldToScreenPoint(marker.node.Position+Vector3.up*1.5f);
                var anchor=new Vector2(Mathf.Clamp(screen.x/layout.scale,layout.map.xMin+8,layout.map.xMax-8),Mathf.Clamp((Screen.height-screen.y)/layout.scale,layout.map.yMin+8,layout.map.yMax-8));
                valid &= Vector2.Distance(marker.anchor,anchor)<.1f && !marker.rect.Overlaps(layout.toolbar) && !marker.rect.Overlaps(layout.footer);
            }
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            if(texture==null)return false;
            var top=texture.GetPixel(texture.width/2,texture.height-4);valid &= Mathf.Max(top.r,top.g,top.b)<.15f;Destroy(texture);
            return valid;
        }
        private static void CaptureFrame(string output,string name,StringBuilder report)
        {
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            if(texture==null){report.AppendLine(name+": framebuffer unavailable");return;}
            var pixels=texture.GetPixels32();int lit=0;for(int i=0;i<pixels.Length;i+=101)if(pixels[i].r>8 || pixels[i].g>8 || pixels[i].b>8)lit++;
            if(lit>pixels.Length/101/5){File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());report.AppendLine(name+": captured framebuffer with IMGUI");}
            else report.AppendLine(name+": hidden-window framebuffer black; no visual verification claimed");
            Destroy(texture);
        }
    }
}
