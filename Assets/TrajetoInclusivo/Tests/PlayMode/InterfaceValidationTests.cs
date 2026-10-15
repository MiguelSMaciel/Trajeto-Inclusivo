using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrajetoInclusivo.Tests
{
    public sealed class InterfaceValidationTests
    {
        [UnityTest] public IEnumerator SyntheticMatrixAllScenariosAndProfiles()=>Matrix("AvenidaVitoria_Testbed");
        [UnityTest] public IEnumerator GeographicMatrixAllScenariosAndProfiles()=>Matrix("AvenidaVitoria_Geografico");
        private IEnumerator Matrix(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var ui=Object.FindAnyObjectByType<SimulationUI>();
            if(sim.geographicContext!=null){sim.geographicContext.SetAssessmentMode(true,sim);yield return null;}
            var report=new StringBuilder("scene,scenario,profile,conventional_m,accessible_m,blocked_on_conventional\n");
            float baseline=0;
            for(int scenario=0;scenario<5;scenario++)for(int profile=0;profile<3;profile++)
            {
                sim.scenarios.Apply(scenario);sim.SetProfile(profile);yield return null;yield return null;
                string label=$"{scene} scenario={scenario} profile={profile}";
                Assert.That(sim.conventional.Found && sim.accessible.Found,Is.True,label);
                if(scenario==0 && profile==0)baseline=sim.conventional.distance;
                Assert.That(sim.accessible.arcs.All(a=>AccessibilityEvaluator.Evaluate(a.segment,sim.Profile,a.forward).Passable),Is.True,label);
                Assert.That(sim.conventional.arcs.Any(a=>a.segment.HasClosure),Is.False,label);
                Assert.That(sim.accessible.nodes.First(),Is.SameAs(sim.origin));Assert.That(sim.accessible.nodes.Last(),Is.SameAs(sim.destination));
                Assert.That(sim.accessible.distance,Is.EqualTo(sim.accessible.arcs.Sum(a=>a.segment.Length)).Within(.01));
                Assert.That(sim.comparison.accessible.blockedCount+sim.comparison.accessible.unknownCount,Is.Zero,label);
                Assert.That(sim.comparison.accessible.minClearance,Is.GreaterThanOrEqualTo(sim.Profile.requiredWidth-.001f));
                if(scenario==0)Assert.That(sim.accessible.distance,Is.EqualTo(sim.conventional.distance).Within(.02),label);
                if(scenario>=1 && scenario<=3 && profile>0)
                {
                    Assert.That(sim.accessible.distance,Is.GreaterThan(baseline+1),label);
                    Assert.That(sim.comparison.conventional.blockedCount,Is.GreaterThan(0),label);
                    if(sim.geographicContext==null)Assert.That(sim.accessible.distance,Is.EqualTo(148).Within(.02),label);
                }
                if(scenario==4)Assert.That(sim.conventional.distance,Is.GreaterThan(baseline+1),label);
                var summaries=ui.GetProfileComparisons();Assert.That(summaries.Length,Is.EqualTo(3));
                Assert.That(summaries[profile],Is.SameAs(sim.comparison));Assert.That(ui.GetProfileComparisons(),Is.SameAs(summaries),"GUI deve reutilizar a comparação até a próxima alteração.");
                report.AppendLine(string.Join(",",scene,scenario+1,profile,sim.conventional.distance.ToString("F3",CultureInfo.InvariantCulture),sim.accessible.distance.ToString("F3",CultureInfo.InvariantCulture),sim.comparison.conventional.blockedCount));
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/Stage4-"+scene+"-Matrix.csv",report.ToString());LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator EndpointPickingCancellationIssueFocusAndCacheInvalidation()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Testbed");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var ui=Object.FindAnyObjectByType<SimulationUI>();var camera=Object.FindAnyObjectByType<CameraController>();
            var originalDestination=sim.destination;var node=sim.graph.nodes.First(n=>n!=sim.origin && n!=sim.destination);
            ui.BeginPicking(true);Physics.SyncTransforms();Assert.That(ui.PickingEndpoint,Is.True);
            Assert.That(ui.HandleMapRay(new Ray(node.Position+Vector3.up*5,Vector3.down)),Is.True,"Raycast deve selecionar um nó visível.");
            yield return null;yield return null;Assert.That(sim.origin,Is.SameAs(node));Assert.That(ui.PickingEndpoint,Is.False);
            ui.BeginPicking(false);ui.CancelPicking();Assert.That(sim.destination,Is.SameAs(originalDestination));
            ui.BeginPicking(false);Assert.That(ui.HandleMapRay(new Ray(node.Position+Vector3.up*5,Vector3.down)),Is.True);
            yield return null;yield return null;Assert.That(sim.accessible.distance,Is.Zero);Assert.That(sim.comparison.ExtraPercent,Is.Zero);
            var old=ui.GetProfileComparisons();sim.SetProfile(2);yield return null;yield return null;
            Assert.That(ui.GetProfileComparisons(),Is.Not.SameAs(old));
            var segment=sim.graph.segments.First(s=>s.id=="C-M");float oldDistance=camera.distance;
            ui.InspectSegment(segment);yield return null;yield return null;
            Assert.That(sim.selectedSegment,Is.SameAs(segment));Assert.That(Vector3.Distance(camera.target,(segment.start.Position+segment.end.Position)*.5f),Is.LessThan(.001));
            Assert.That(camera.distance,Is.LessThan(oldDistance));
            Assert.That(ui.PickingEndpoint,Is.False);LogAssert.NoUnexpectedReceived();
        }
    }
}
