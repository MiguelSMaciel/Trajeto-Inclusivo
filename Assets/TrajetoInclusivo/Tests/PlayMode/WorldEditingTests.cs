using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrajetoInclusivo.Tests
{
    public sealed class WorldEditingTests
    {
        [UnityTest] public IEnumerator EditsRerouteUndoRedoAndPersistWithPhysicalGeometry()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Testbed");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();sim.scenarios.Apply(0);yield return null;
            var editor=sim.worldEditor;var s=sim.graph.segments.Find(x=>x.id=="C-M");float initial=sim.accessible.distance;
            var hole=editor.AddObstacle(s,ObstacleKind.Pothole);Assert.That(hole,Is.Not.Null);
            Assert.That(editor.Edit("Blocking hole",()=>{hole.occupiedWidth=1.75f;hole.lateralOffset=.325f;hole.heightOrDepth=.2f;}),Is.True);
            yield return null;yield return null;
            Assert.That(s.EffectiveWidth,Is.EqualTo(.65f).Within(.001));Assert.That(sim.accessible.distance,Is.EqualTo(148).Within(.02));
            var floor=ObstacleFootprints.SurfacePoint(s,s.HorizontalDistance*.5f,.325f,.2f);
            var collider=s.transform.Find("Pavimento").GetComponent<MeshCollider>();Physics.SyncTransforms();
            Assert.That(collider.Raycast(new Ray(floor+Vector3.up*5,Vector3.down),out var hit,10),Is.True);
            Assert.That(hit.point.y,Is.EqualTo(floor.y).Within(.001));
            editor.Undo();yield return null;yield return null;
            Assert.That(sim.accessible.distance,Is.LessThan(148));editor.Redo();yield return null;yield return null;
            Assert.That(sim.accessible.distance,Is.EqualTo(148).Within(.02));
            string path=Path.GetFullPath("TestResults/stage3-testbed.experiment.json");Assert.That(editor.Save(path),Is.True,editor.status);
            editor.Edit("Clear",()=>{foreach(var o in s.obstacles.ToArray())sim.scenarios.RemoveObstacle(o);});yield return null;
            Assert.That(sim.accessible.distance,Is.EqualTo(initial).Within(.02));
            Assert.That(editor.Load(path),Is.True,editor.status);yield return null;yield return null;
            Assert.That(s.obstacles.Count,Is.EqualTo(1));Assert.That(s.obstacles[0].heightOrDepth,Is.EqualTo(.2f));
            Assert.That(sim.accessible.distance,Is.EqualTo(148).Within(.02));
            var state=editor.Capture();state.graphFingerprint="wrong";string before=editor.CaptureJson();
            File.WriteAllText(path+".invalid",JsonUtility.ToJson(state));Assert.That(editor.Load(path+".invalid"),Is.False);
            Assert.That(editor.CaptureJson(),Is.EqualTo(before),"Carga inválida não pode modificar o mundo.");
            editor.Undo();yield return null;yield return null;
            Assert.That(sim.accessible.distance,Is.EqualTo(initial).Within(.02));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ElevationSurvivesScenariosSaveLoadAndUndoWithoutClaimingSurveyedData()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Geografico");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var editor=sim.worldEditor;
            var node=sim.origin;float old=node.Position.y;Assert.That(editor.ApplyElevation(editor.exampleElevation),Is.True,editor.status);
            yield return null;yield return null;
            Assert.That(editor.exampleElevation.TrySample(node.Position.x,node.Position.z,out float expected),Is.True);
            Assert.That(node.Position.y,Is.EqualTo(expected).Within(.001));Assert.That(node.Position.y,Is.Not.EqualTo(old));
            Assert.That(sim.accessible.failure,Is.EqualTo(RouteFailure.InsufficientData));
            Assert.That(sim.graph.segments.All(s=>!s.hasAssessmentData),Is.True);
            editor.Edit("Change scenario",()=>sim.scenarios.Apply(2));yield return null;
            Assert.That(sim.scenarios.BaseElevation(node),Is.EqualTo(expected).Within(.001));
            editor.Undo();yield return null;Assert.That(node.Position.y,Is.EqualTo(expected).Within(.001));
            string path=Path.GetFullPath("TestResults/stage3-geographic.experiment.json");Assert.That(editor.Save(path),Is.True,editor.status);
            editor.Undo();yield return null;Assert.That(node.Position.y,Is.EqualTo(old).Within(.001));
            Assert.That(editor.Load(path),Is.True,editor.status);yield return null;yield return null;
            Assert.That(node.Position.y,Is.EqualTo(expected).Within(.001));Assert.That(node.elevationVerification,Is.EqualTo(VerificationState.Simulated));
            editor.Undo();yield return null;Assert.That(node.Position.y,Is.EqualTo(old).Within(.001));LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator RampEditsSharedNodesAndCanBeReversed()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Testbed");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();sim.scenarios.Apply(0);yield return null;
            var s=sim.graph.segments.Find(x=>x.id=="C-M");float old=s.end.Position.y;
            Assert.That(sim.worldEditor.ConfigureRamp(s,s.start.Position.y,s.start.Position.y+s.HorizontalDistance*.12f,2.4f,-2),Is.True);
            yield return null;yield return null;Assert.That(s.SlopePercent,Is.EqualTo(12).Within(.001));Assert.That(sim.accessible.distance,Is.EqualTo(148).Within(.02));
            Assert.That(s.GetComponent<AccessibilityRamp>().LongitudinalSlopePercent,Is.EqualTo(12).Within(.001));
            sim.worldEditor.Undo();yield return null;yield return null;Assert.That(s.end.Position.y,Is.EqualTo(old).Within(.001));
            Assert.That(sim.accessible.distance,Is.LessThan(101));LogAssert.NoUnexpectedReceived();
        }
    }
}
