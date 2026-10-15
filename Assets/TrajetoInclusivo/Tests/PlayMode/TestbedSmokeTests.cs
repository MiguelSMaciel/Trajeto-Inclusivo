using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrajetoInclusivo.Tests
{
    public sealed class TestbedSmokeTests
    {
        [UnityTest]
        public IEnumerator FiveScenariosRunAndRerouteInTheActualScene()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Testbed");
            yield return null;
            var sim = Object.FindAnyObjectByType<SimulationController>();
            Assert.That(sim, Is.Not.Null);
            Assert.That(sim.graph.nodes.Count, Is.EqualTo(13));
            sim.SetProfile(1);
            for (int i = 0; i < 5; i++)
            {
                sim.scenarios.Apply(i);
                yield return null; yield return null;
                Assert.That(sim.accessible.Found, Is.True, "Cenário " + (i+1));
                Assert.That(sim.conventional.Found, Is.True);
                if (i == 0) Assert.That(sim.accessible.distance, Is.LessThan(101));
                if (i >= 1 && i <= 3)
                {
                    Assert.That(sim.conventional.distance, Is.LessThan(101));
                    Assert.That(sim.accessible.distance, Is.EqualTo(148).Within(.02));
                }
                if (i == 4) Assert.That(sim.conventional.distance, Is.EqualTo(148).Within(.02));
                if (i == 2)
                {
                    var ramp = sim.graph.segments.Find(s => s.id == "C-M");
                    Assert.That(ramp.SlopePercent, Is.EqualTo(12).Within(.001));
                    var mesh = ramp.transform.Find("Pavimento").GetComponent<MeshCollider>().sharedMesh;
                    Assert.That(mesh.bounds.max.y, Is.GreaterThan(2.5f));
                }
            }
            sim.scenarios.Apply(1);
            yield return null; yield return null;
            var segment = sim.graph.segments.Find(s => s.id == "C-M");
            sim.scenarios.RemoveObstacle(segment.obstacles[0]);
            yield return null; yield return null;
            Assert.That(sim.accessible.distance, Is.LessThan(101), "Remover degrau deve restaurar a rota direta.");
            sim.scenarios.Apply(1); sim.SetProfile(0);
            yield return null; yield return null;
            Assert.That(sim.accessible.distance, Is.LessThan(101), "Perfil pedestre deve superar o degrau de teste.");
            sim.SetProfile(1);
            yield return null; yield return null;
            var inspection = Object.FindAnyObjectByType<RouteInspection>();
            inspection.Begin();
            var start = inspection.marker.position;
            yield return new WaitForSeconds(.2f);
            Assert.That(Vector3.Distance(start, inspection.marker.position), Is.GreaterThan(.1));
            inspection.Stop();
            sim.selectedSegment = segment;
            var camera = Object.FindAnyObjectByType<CameraController>(); camera.topDown = true;
            yield return null;
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Directory.CreateDirectory("TestResults");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("TestResults/Testbed-PlayMode.png"));
                for (int i = 0; i < 8; i++) yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }
    }
}
