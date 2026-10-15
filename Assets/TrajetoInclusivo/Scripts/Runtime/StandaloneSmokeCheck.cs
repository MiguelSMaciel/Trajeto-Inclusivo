using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrajetoInclusivo
{
    // Opt-in QA entry point for the built player. Normal launches create no reports or screenshots.
    public sealed class StandaloneSmokeCheck : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-stage4-smoke") >= 0) return;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-testbed-smoke") < 0) return;
            Application.runInBackground = true;
            new GameObject("Standalone smoke check").AddComponent<StandaloneSmokeCheck>();
        }
        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int outputArgument = Array.IndexOf(arguments, "-testbed-results");
            string output = outputArgument >= 0 && outputArgument + 1 < arguments.Length ? arguments[outputArgument + 1] : Application.persistentDataPath;
            Directory.CreateDirectory(output);
            Screen.SetResolution(1600, 1000, FullScreenMode.Windowed);
            yield return null; yield return null;
            var sim = FindAnyObjectByType<SimulationController>();
            var report = new System.Text.StringBuilder();
            bool passed = true;
            var geo = sim.geographicContext;
            if (geo != null)
            {
                bool unknown = !sim.accessible.Found && sim.accessible.failure == RouteFailure.InsufficientData;
                passed &= unknown; report.AppendLine("Initial unknown assessment: " + (unknown ? "PASS" : "FAIL"));
                geo.SetAssessmentMode(true, sim); yield return null; yield return null;
            }
            float baseline = 0;
            for (int i = 0; i < 5; i++)
            {
                sim.scenarios.Apply(i); yield return null; yield return null;
                if (i == 0) baseline = sim.conventional.distance;
                bool ok = sim.accessible.Found && sim.conventional.Found;
                if (geo == null) ok &= i == 0 ? sim.accessible.distance < 101 : Mathf.Abs(sim.accessible.distance - 148) < .02f;
                else ok &= i == 0 ? Mathf.Abs(sim.accessible.distance - baseline) < .02f : sim.accessible.distance > baseline + 1;
                passed &= ok;
                report.AppendLine($"Scenario {i+1}: {(ok ? "PASS" : "FAIL")} conventional={sim.conventional.distance:F3}m accessible={sim.accessible.distance:F3}m");
            }
            sim.scenarios.Apply(1); yield return null;
            var cameraController = FindAnyObjectByType<CameraController>(); cameraController.FrameMap();
            yield return new WaitForSeconds(.5f);
            yield return new WaitForEndOfFrame();
            string prefix = geo == null ? "Testbed" : "Geographic";
            CaptureGeometry(cameraController.view, Path.Combine(output, prefix + "-Perspectiva.png"));
            yield return null; yield return null;
            cameraController.topDown = true;
            yield return new WaitForSeconds(.2f);
            yield return new WaitForEndOfFrame();
            CaptureGeometry(cameraController.view, Path.Combine(output, prefix + "-Superior.png"));
            yield return null; yield return null;
            if (geo != null)
            {
                cameraController.FrameRoute(sim.accessible);
                yield return new WaitForSeconds(.2f); yield return new WaitForEndOfFrame();
                CaptureGeometry(cameraController.view, Path.Combine(output, "Geographic-Rota.png"));
                cameraController.topDown = false;
                yield return new WaitForSeconds(.2f); yield return new WaitForEndOfFrame();
                CaptureGeometry(cameraController.view, Path.Combine(output, "Geographic-Rota3D.png"));
            }
            sim.scenarios.Apply(0);yield return null;yield return null;
            var editor=sim.worldEditor;
            var segment=sim.conventional.arcs.Select(a=>a.segment).First(s=>!s.syntheticConnection && s.HorizontalDistance>8);
            var hole=editor.AddObstacle(segment,ObstacleKind.Pothole);
            bool physical=hole!=null && editor.Edit("QA buraco",()=>{hole.occupiedWidth=segment.totalWidth-.65f;hole.lateralOffset=.325f;hole.heightOrDepth=.2f;hole.positionAlongSegment=.45f;hole.length=2;});
            yield return null;yield return null;
            var center=ObstacleFootprints.Bounds(hole).center;var floor=ObstacleFootprints.SurfacePoint(segment,center.x,center.y,.2f);
            var collider=segment.transform.Find("Pavimento").GetComponent<MeshCollider>();Physics.SyncTransforms();
            physical &= collider.Raycast(new Ray(floor+Vector3.up*5,Vector3.down),out var floorHit,10) && Mathf.Abs(floorHit.point.y-floor.y)<.001f && Mathf.Abs(segment.EffectiveWidth-.65f)<.001f;
            passed &= physical;report.AppendLine("Stage 3 physical pothole and continuous clearance: "+(physical?"PASS":"FAIL"));
            string experimentPath=Path.Combine(output,"Stage3-Player.experiment.json");string expected=editor.CaptureJson();
            bool persistence=editor.Save(experimentPath);editor.Undo();yield return null;editor.Redo();yield return null;
            persistence &= editor.Load(experimentPath);yield return null;yield return null;
            persistence &= editor.CaptureJson()==expected;passed &= persistence;report.AppendLine("Stage 3 undo/redo and saved experiment: "+(persistence?"PASS":"FAIL"));
            foreach(var type in new[]{ObstacleKind.Pole,ObstacleKind.Tree,ObstacleKind.Furniture})
            {
                var o=editor.AddObstacle(segment,type);float position=type==ObstacleKind.Pole?.2f:type==ObstacleKind.Tree?.75f:.92f;
                editor.Edit("QA volume",()=>{o.positionAlongSegment=position;o.occupiedWidth=.5f;o.length=.6f;o.lateralOffset=-segment.totalWidth*.5f+.25f;});
            }
            bool ramp=editor.ConfigureRamp(segment,segment.start.Position.y,segment.start.Position.y+segment.HorizontalDistance*.06f,segment.totalWidth,-2);
            yield return null;yield return null;
            ramp &= Mathf.Abs(segment.SlopePercent-6)<.001f;passed &= ramp;report.AppendLine("Stage 3 ramp geometry: "+(ramp?"PASS":"FAIL"));
            cameraController.topDown=false;cameraController.followTarget=null;cameraController.target=ObstacleFootprints.SurfacePoint(segment,segment.HorizontalDistance*.5f,0);
            cameraController.distance=Mathf.Clamp(segment.HorizontalDistance*1.3f,12,25);cameraController.pitch=48;
            var direction=segment.end.Position-segment.start.Position;cameraController.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg+65;
            yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();CaptureGeometry(cameraController.view,Path.Combine(output,"Stage3-Obstaculos.png"));
            bool elevation=editor.ApplyElevation(editor.exampleElevation);yield return null;yield return null;
            var probe=sim.graph.nodes.First(n=>editor.exampleElevation.TrySample(n.Position.x,n.Position.z,out _));
            editor.exampleElevation.TrySample(probe.Position.x,probe.Position.z,out float sampled);
            elevation &= Mathf.Abs(sim.scenarios.BaseElevation(probe)-sampled)<.001f;passed &= elevation;report.AppendLine("Stage 3 synthetic elevation grid: "+(elevation?"PASS":"FAIL"));
            cameraController.FrameRoute(sim.conventional);cameraController.topDown=false;
            yield return new WaitForSeconds(.2f);yield return new WaitForEndOfFrame();CaptureGeometry(cameraController.view,Path.Combine(output,"Stage3-Relevo.png"));
            sim.selectedSegment=segment;
            yield return new WaitForSeconds(.2f);
            yield return new WaitForEndOfFrame();
            report.AppendLine("Capturas geométricas offscreen: não incluem a interface IMGUI.");
            yield return new WaitForSeconds(.3f);
            File.WriteAllText(Path.Combine(output, geo == null ? "StandaloneSmoke.txt" : "GeographicSmoke.txt"), report.ToString());
            Debug.Log(passed ? "TRAJETO_PLAYER_SMOKE_OK" : "TRAJETO_PLAYER_SMOKE_FAILED");
            Application.Quit(passed ? 0 : 1);
        }
        private static void CaptureGeometry(Camera view, string path)
        {
            var viewport = view.rect;
            float originalAspect = view.aspect;
            var target = new RenderTexture(Mathf.RoundToInt(1600 * viewport.width), 1000, 24, RenderTextureFormat.ARGB32);
            view.rect = new Rect(0, 0, 1, 1);
            view.aspect = target.width / (float)target.height;
            target.Create();
            RenderPipeline.SubmitRenderRequest(view, new RenderPipeline.StandardRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            RenderTexture.active = previous; view.rect = viewport; view.aspect = originalAspect; target.Release(); Destroy(target); Destroy(pixels);
        }
    }
}
