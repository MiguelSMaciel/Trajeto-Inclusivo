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
    public sealed class FaesaSceneTests
    {
        [UnityTest] public IEnumerator ExpandedSceneRoutesFromPlazaAndIfesForAllScenariosAndProfiles()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_FAESA");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var context=sim.geographicContext;var ui=Object.FindAnyObjectByType<SimulationUI>();
            Assert.That(context.faesa,Is.Not.Null);Assert.That(sim.destination,Is.SameAs(context.faesa));Assert.That(sim.origin,Is.SameAs(context.plaza));
            Assert.That(sim.accessible.failure,Is.EqualTo(RouteFailure.InsufficientData));Assert.That(sim.graph.segments.All(s=>!s.hasAssessmentData),Is.True);
            Assert.That(context.buildingFeatureCount,Is.GreaterThan(1100));Assert.That(sim.graph.nodes.Select(n=>n.id).Distinct().Count(),Is.EqualTo(sim.graph.nodes.Count));
            Assert.That(context.faesa.displayName,Does.Contain("referência aproximada"));
            Assert.That(sim.graph.Neighbors(context.faesa).All(a=>a.segment.HorizontalDistance>.001f),Is.True,"Referência projetada não deve criar uma aresta de acesso com comprimento zero.");
            Assert.That(sim.graph.Neighbors(context.faesa).Any(a=>a.segment.displayName=="Avenida Vitória"),Is.True);
            context.SetAssessmentMode(true,sim);yield return null;
            var report=new StringBuilder("origin,scenario,profile,conventional_m,accessible_m\n");
            for(int origin=0;origin<2;origin++)for(int scenario=0;scenario<5;scenario++)for(int profile=0;profile<3;profile++)
            {
                ui.SetFaesaRoute(origin==1);sim.scenarios.Apply(scenario);sim.SetProfile(profile);yield return null;yield return null;
                string label=$"FAESA origin={origin} scenario={scenario} profile={profile}";
                Assert.That(sim.origin,Is.SameAs(origin==1?context.ifes:context.plaza));Assert.That(sim.destination,Is.SameAs(context.faesa));
                Assert.That(sim.conventional.Found && sim.accessible.Found,Is.True,label);
                Assert.That(sim.accessible.arcs.All(a=>AccessibilityEvaluator.Evaluate(a.segment,sim.Profile,a.forward).Passable),Is.True,label);
                Assert.That(sim.conventional.arcs.Any(a=>a.segment.HasClosure),Is.False,label);
                Assert.That(sim.accessible.distance,Is.GreaterThan(300),label);
                if(origin==0 && scenario>=1 && scenario<=3 && profile>0)Assert.That(sim.accessible.distance,Is.GreaterThan(sim.conventional.distance+1),label);
                report.AppendLine(string.Join(",",origin==0?"Praca":"Ifes",scenario+1,profile,sim.conventional.distance.ToString("F3",CultureInfo.InvariantCulture),sim.accessible.distance.ToString("F3",CultureInfo.InvariantCulture)));
            }
            Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/FAESA-Matrix.csv",report.ToString());
            ui.SetFaesaRoute(false);sim.scenarios.Apply(0);yield return null;yield return null;
            var camera=Object.FindAnyObjectByType<CameraController>();camera.FrameRoute(sim.accessible);yield return null;yield return null;
            foreach(var n in sim.accessible.nodes){var p=camera.view.WorldToViewportPoint(n.Position);Assert.That(p.x,Is.InRange(0f,1f));Assert.That(p.y,Is.InRange(0f,1f));}
            context.SetAssessmentMode(false,sim);yield return null;Assert.That(sim.accessible.failure,Is.EqualTo(RouteFailure.InsufficientData));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
