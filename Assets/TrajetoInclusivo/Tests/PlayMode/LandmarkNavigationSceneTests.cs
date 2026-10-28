using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TrajetoInclusivo.Tests
{
    public sealed class LandmarkNavigationSceneTests
    {
        [UnityTest] public IEnumerator LandmarkFocusAndQuickRoutesPreserveEndpointsAndAssessment()
        {
            yield return SceneManager.LoadSceneAsync("AvenidaVitoria_FAESA");yield return null;yield return null;
            var sim=Object.FindAnyObjectByType<SimulationController>();var ui=Object.FindAnyObjectByType<SimulationUI>();var camera=Object.FindAnyObjectByType<CameraController>();
            var context=sim.geographicContext;var a=sim.origin;var b=sim.destination;var originalRoute=sim.conventional;
            var layout=new SimulationLayout(Screen.width,Screen.height);
            Assert.That(ui.IsPointerOverUI(new Vector2(Screen.width*.5f,Screen.height-4)),Is.True,"A margem fora do mapa não deve selecionar geometria escondida.");
            foreach(var node in new[]{context.plaza,context.ifes,context.faesa})
            {
                ui.BeginPicking(true);ui.FocusLandmark(node);yield return null;yield return null;
                Assert.That(ui.PickingEndpoint,Is.False);Assert.That(camera.followTarget,Is.Null);
                Assert.That(Vector3.Distance(camera.target,node.Position),Is.LessThan(.001));
                Assert.That(sim.origin,Is.SameAs(a));Assert.That(sim.destination,Is.SameAs(b));Assert.That(sim.conventional,Is.SameAs(originalRoute),"Somente navegar não deve recalcular rotas.");
                Assert.That(sim.accessible.failure,Is.EqualTo(RouteFailure.InsufficientData));
                var markers=ui.GetLandmarkMarkers();Assert.That(markers.Any(m=>m.node==node),Is.True);
                foreach(var marker in markers)
                {
                    var projected=camera.view.WorldToScreenPoint(marker.node.Position+Vector3.up*1.5f);
                    var expected=new Vector2(projected.x/layout.scale,(Screen.height-projected.y)/layout.scale);
                    expected=new Vector2(Mathf.Clamp(expected.x,layout.map.xMin+8,layout.map.xMax-8),Mathf.Clamp(expected.y,layout.map.yMin+8,layout.map.yMax-8));
                    Assert.That(Vector2.Distance(marker.anchor,expected),Is.LessThan(.05f),"O rótulo deve apontar para o nó após recortar o viewport.");
                    Assert.That(marker.rect.Overlaps(layout.left)||marker.rect.Overlaps(layout.right)||marker.rect.Overlaps(layout.toolbar)||marker.rect.Overlaps(layout.footer),Is.False);
                    var screen=new Vector2(marker.rect.center.x*layout.scale,Screen.height-marker.rect.center.y*layout.scale);
                    Assert.That(ui.IsPointerOverUI(screen),Is.True,"Clique no rótulo não pode selecionar a geometria por trás.");
                }
            }
            ui.SetFaesaRoute(true);yield return null;yield return null;
            Assert.That(sim.origin,Is.SameAs(context.ifes));Assert.That(sim.destination,Is.SameAs(context.faesa));
            var route=sim.conventional;var bounds=new Bounds(route.nodes[0].Position,Vector3.zero);foreach(var node in route.nodes)bounds.Encapsulate(node.Position);
            Assert.That(Vector3.Distance(camera.target,bounds.center),Is.LessThan(.001));
            var footer=layout.footer.center;Assert.That(ui.IsPointerOverUI(new Vector2(footer.x*layout.scale,Screen.height-footer.y*layout.scale)),Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
