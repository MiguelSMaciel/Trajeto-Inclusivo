using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrajetoInclusivo.Tests
{
    public sealed class GeographicSceneTests
    {
        [UnityTest] public IEnumerator ImportedMapStartsUnknownAndSyntheticScenariosReroute()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Geografico"); yield return null; yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var context=sim.geographicContext;
            Assert.That(context,Is.Not.Null);Assert.That(context.buildingFeatureCount,Is.GreaterThan(700));
            Assert.That(context.useSimulatedAssessment,Is.False);Assert.That(sim.conventional.Found,Is.True);
            Assert.That(sim.accessible.failure,Is.EqualTo(RouteFailure.InsufficientData));
            Assert.That(sim.graph.segments.All(s=>!s.hasAssessmentData),Is.True);
            context.SetAssessmentMode(true,sim);yield return null;yield return null;
            Assert.That(sim.accessible.Found,Is.True);float baseline=sim.conventional.distance;
            var camera=Object.FindAnyObjectByType<CameraController>();
            camera.FrameRoute(sim.accessible);yield return null;yield return null;
            foreach(var node in sim.accessible.nodes)
            {
                var viewport=camera.view.WorldToViewportPoint(node.Position);
                Assert.That(viewport.x,Is.InRange(0f,1f),"Nó fora do enquadramento horizontal: "+node.id);
                Assert.That(viewport.y,Is.InRange(0f,1f),"Nó fora do enquadramento vertical: "+node.id);
            }
            var s=sim.graph.segments.First(x=>!x.syntheticConnection);
            var existingMesh=s.transform.Find("Pavimento").GetComponent<MeshFilter>().sharedMesh;
            sim.SetProfile(2);yield return null;yield return null;
            Assert.That(s.transform.Find("Pavimento").GetComponent<MeshFilter>().sharedMesh,Is.SameAs(existingMesh),"Mudar perfil não deve reconstruir pavimento.");
            sim.SetProfile(1);
            for(int i=0;i<5;i++)
            {
                sim.scenarios.Apply(i);yield return null;yield return null;
                Assert.That(sim.accessible.Found,Is.True,"Cenário geográfico "+(i+1));
                Assert.That(sim.conventional.Found,Is.True);
                if(i==0)Assert.That(sim.accessible.distance,Is.EqualTo(baseline).Within(.02));
                if(i>=1 && i<=3)Assert.That(sim.accessible.distance,Is.GreaterThan(sim.conventional.distance+1));
                if(i==4)Assert.That(sim.conventional.distance,Is.GreaterThan(baseline+1));
                if(i==2)
                {
                    string node=sim.scenarios.scenarios[i].raisedNodeId;
                    Assert.That(sim.graph.segments.Any(edge=>edge.end.id==node && Mathf.Abs(edge.SlopePercent-12)<.001f),Is.True);
                }
                camera.topDown=i%2==0;camera.FrameRoute(sim.accessible);yield return null;yield return null;
                foreach(var node in sim.accessible.nodes)
                {
                    var viewport=camera.view.WorldToViewportPoint(node.Position);
                    Assert.That(viewport.x,Is.InRange(0f,1f),"Desvio fora do enquadramento: "+node.id);
                    Assert.That(viewport.y,Is.InRange(0f,1f),"Desvio fora do enquadramento: "+node.id);
                }
            }
            context.SetAssessmentMode(false,sim);yield return null;yield return null;
            Assert.That(sim.accessible.failure,Is.EqualTo(RouteFailure.InsufficientData));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
