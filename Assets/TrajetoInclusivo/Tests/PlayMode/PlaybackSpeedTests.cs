using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrajetoInclusivo.Tests
{
    public sealed class PlaybackSpeedTests
    {
        private IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_Testbed");
            yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();
            sim.scenarios.Apply(0);sim.SetProfile(1);
            yield return null;yield return null;
        }

        [UnityTest]
        public IEnumerator ChangingSpeedDuringPlaybackPreservesRouteAndAppliesMultiplier()
        {
            yield return LoadScene();
            var sim=Object.FindAnyObjectByType<SimulationController>();
            var inspection=Object.FindAnyObjectByType<RouteInspection>();
            var route=sim.accessible;float timeScale=Time.timeScale;
            Assert.That(inspection.PlaybackSpeedMultiplier,Is.EqualTo(1));
            inspection.speedMetresPerSecond=.1f;
            inspection.Begin();
            float expected=0;var start=inspection.marker.position;
            float[] multipliers={.5f,1f,1.5f,2f,4f,8f};
            for(int index=0;index<multipliers.Length;index++)
            {
                var before=inspection.marker.position;
                inspection.SetPlaybackSpeedIndex(index);
                Assert.That(inspection.marker.position,Is.EqualTo(before),"Trocar velocidade não deve reposicionar a cadeira.");
                for(int frame=0;frame<3;frame++){yield return null;expected+=.1f*multipliers[index]*Time.deltaTime;}
                Assert.That(Vector3.Distance(start,inspection.marker.position),Is.EqualTo(expected).Within(.002),"Multiplicador "+multipliers[index]);
                Assert.That(inspection.Running,Is.True);
                Assert.That(sim.accessible,Is.SameAs(route),"Reprodução não deve recalcular a rota.");
                Assert.That(Time.timeScale,Is.EqualTo(timeScale));
                Assert.That(inspection.cameraController.followTarget,Is.SameAs(inspection.marker));
            }
            inspection.Stop();LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SelectedSpeedSurvivesStopRestartAndRerouting()
        {
            yield return LoadScene();
            var sim=Object.FindAnyObjectByType<SimulationController>();
            var inspection=Object.FindAnyObjectByType<RouteInspection>();
            inspection.SetPlaybackSpeedIndex(3);inspection.Begin();
            yield return null;
            inspection.Stop();inspection.Begin();
            Assert.That(inspection.PlaybackSpeedMultiplier,Is.EqualTo(2));
            Assert.That(inspection.marker.position,Is.EqualTo(sim.origin.Position));
            sim.scenarios.Apply(1);yield return null;yield return null;
            Assert.That(inspection.Running,Is.False,"Alterar a rota continua encerrando a inspeção.");
            Assert.That(inspection.PlaybackSpeedMultiplier,Is.EqualTo(2));
            inspection.Begin();Assert.That(inspection.Running,Is.True);
            inspection.SetPlaybackSpeedIndex(1);Assert.That(inspection.PlaybackSpeedMultiplier,Is.EqualTo(1));
            inspection.Stop();LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator FastPlaybackFinishesExactlyAtDestination()
        {
            yield return LoadScene();
            var sim=Object.FindAnyObjectByType<SimulationController>();
            var inspection=Object.FindAnyObjectByType<RouteInspection>();
            inspection.speedMetresPerSecond=100000;inspection.SetPlaybackSpeedIndex(5);inspection.Begin();
            for(int frame=0;frame<5 && inspection.Running;frame++)yield return null;
            Assert.That(inspection.Running,Is.False);
            Assert.That(inspection.marker.position,Is.EqualTo(sim.destination.Position));
            Assert.That(inspection.cameraController.followTarget,Is.Null);
            Assert.That(inspection.PlaybackSpeedMultiplier,Is.EqualTo(8));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
