using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrajetoInclusivo.Tests
{
    public sealed class NodeInspectionTests
    {
        [UnityTest] public IEnumerator ClickInspectsWithoutEditingAndEndpointPickingKeepsPriority()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Testbed");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var ui=Object.FindAnyObjectByType<SimulationUI>();
            var node=sim.graph.nodes.Single(n=>n.id=="M");var origin=sim.origin;var destination=sim.destination;
            var route=sim.conventional;string world=sim.worldEditor.CaptureJson();
            Physics.SyncTransforms();Assert.That(ui.HandleMapRay(new Ray(node.Position+Vector3.up*5,Vector3.down)),Is.True);
            yield return null;yield return null;
            Assert.That(ui.SelectedNode,Is.SameAs(node));Assert.That(sim.selectedSegment,Is.Null);
            Assert.That(sim.origin,Is.SameAs(origin));Assert.That(sim.destination,Is.SameAs(destination));
            Assert.That(sim.conventional,Is.SameAs(route));Assert.That(sim.worldEditor.CaptureJson(),Is.EqualTo(world));
            CollectionAssert.AreEquivalent(new[]{"C-M","M-E"},ui.ConnectedSegments(node).Select(s=>s.id));
            var ring=sim.visualizer.transform.Find("Nó inspecionado");Assert.That(ring.gameObject.activeSelf,Is.True);
            sim.scenarios.Apply(2);yield return null;yield return null;
            Assert.That(ring.position,Is.EqualTo(node.Position));
            ui.UseInspectedNodeAsEndpoint(true);yield return null;yield return null;
            Assert.That(sim.origin,Is.SameAs(node));Assert.That(sim.destination,Is.SameAs(destination));
            ui.UseInspectedNodeAsEndpoint(false);yield return null;yield return null;
            Assert.That(sim.accessible.Found,Is.True);Assert.That(sim.accessible.distance,Is.Zero);
            ui.BeginPicking(true);Assert.That(ui.SelectedNode,Is.Null);Assert.That(ring.gameObject.activeSelf,Is.False);
            Physics.SyncTransforms();Assert.That(ui.HandleMapRay(new Ray(origin.Position+Vector3.up*5,Vector3.down)),Is.True);
            yield return null;yield return null;
            Assert.That(sim.origin,Is.SameAs(origin));Assert.That(ui.SelectedNode,Is.Null);Assert.That(ui.PickingEndpoint,Is.False);
            ui.InspectNode(node);ui.InspectSegment(ui.ConnectedSegments(node)[0],false);
            Assert.That(ui.SelectedNode,Is.Null);Assert.That(sim.selectedSegment,Is.Not.Null);Assert.That(ring.gameObject.activeSelf,Is.False);
            ui.InspectNode(node);ui.ShowProfileComparison(true);Assert.That(ui.SelectedNode,Is.Null);
            ui.InspectNode(node);ui.CloseInspection();Assert.That(ui.SelectedNode,Is.Null);Assert.That(sim.selectedSegment,Is.Null);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ScenarioLocatorDistinguishesNodeElevationFromSegmentObstacles()
        {
            foreach(string scene in new[]{"AvenidaVitoria_Testbed","AvenidaVitoria_Geografico","AvenidaVitoria_FAESA"})
            {
                yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return null;
                var sim=Object.FindAnyObjectByType<SimulationController>();var ui=Object.FindAnyObjectByType<SimulationUI>();var camera=Object.FindAnyObjectByType<CameraController>();
                sim.scenarios.Apply(2);yield return null;yield return null;
                var scenario=sim.scenarios.scenarios[2];var raised=sim.graph.nodes.Single(n=>n.id==scenario.raisedNodeId);
                Physics.SyncTransforms();Assert.That(ui.HandleMapRay(new Ray(raised.Position+Vector3.up*5,Vector3.down)),Is.True);
                Assert.That(ui.SelectedNode,Is.SameAs(raised),"O nó elevado deve ter uma bolinha clicável também nos mapas geográficos.");
                Assert.That(ui.HasScenarioInspectionTarget,Is.True);ui.InspectScenarioChange();
                Assert.That(ui.SelectedNode,Is.SameAs(raised));Assert.That(Vector3.Distance(camera.target,raised.Position),Is.LessThan(.001));
                Assert.That(raised.Position.y-sim.scenarios.BaseElevation(raised),Is.EqualTo(scenario.elevationOffset).Within(.001));
                sim.worldEditor.ConfigureRamp(ui.ConnectedSegments(raised)[0],2,3,2.4f,0);yield return null;
                sim.scenarios.Apply(1);yield return null;yield return null;ui.InspectScenarioChange();
                Assert.That(ui.SelectedNode,Is.Null);Assert.That(sim.selectedSegment.id,Is.EqualTo(sim.scenarios.scenarios[1].obstacles[0].segmentId));
                Assert.That(Vector3.Distance(camera.target,(sim.selectedSegment.start.Position+sim.selectedSegment.end.Position)*.5f),Is.LessThan(.001));
                LogAssert.NoUnexpectedReceived();
            }
        }
        [UnityTest] public IEnumerator GeographicNodeInspectionPreservesUnverifiedAssessmentAndRejectsForeignNodes()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_FAESA");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var ui=Object.FindAnyObjectByType<SimulationUI>();
            var node=sim.geographicContext.faesa;var route=sim.conventional;
            Physics.SyncTransforms();Assert.That(ui.HandleMapRay(new Ray(node.Position+Vector3.up*5,Vector3.down)),Is.True);
            Assert.That(ui.SelectedNode,Is.SameAs(node));
            Assert.That(ui.ConnectedSegments(node).Length,Is.GreaterThan(0));
            Assert.That(sim.geographicContext.useSimulatedAssessment,Is.False);Assert.That(sim.accessible.failure,Is.EqualTo(RouteFailure.InsufficientData));
            var foreign=new GameObject("Fora da rede").AddComponent<PedestrianNode>();
            ui.InspectNode(foreign);Assert.That(ui.SelectedNode,Is.SameAs(node));Object.Destroy(foreign.gameObject);
            ui.CloseInspection();yield return null;Assert.That(sim.conventional,Is.SameAs(route));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
