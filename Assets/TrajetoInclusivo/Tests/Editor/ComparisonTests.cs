using NUnit.Framework;
using UnityEngine;

namespace TrajetoInclusivo.Tests
{
    public sealed class ComparisonTests
    {
        private GameObject root;
        private PedestrianGraph graph;
        private PedestrianNode a,b,c;
        private AccessibilitySegment direct;
        private AccessibilityProfile profile;
        [SetUp] public void Setup()
        {
            root=new GameObject("Comparison");graph=root.AddComponent<PedestrianGraph>();profile=ScriptableObject.CreateInstance<AccessibilityProfile>();
            a=Node("A",Vector3.zero);b=Node("B",new Vector3(10,.5f,0));c=Node("C",new Vector3(5,.25f,6));
            direct=Edge(a,b);Edge(a,c);Edge(c,b);graph.Rebuild();
        }
        [TearDown] public void TearDown(){Object.DestroyImmediate(root);Object.DestroyImmediate(profile);}
        private PedestrianNode Node(string id,Vector3 p){var go=new GameObject(id);go.transform.SetParent(root.transform);go.transform.position=p;var n=go.AddComponent<PedestrianNode>();n.id=id;graph.nodes.Add(n);return n;}
        private AccessibilitySegment Edge(PedestrianNode x,PedestrianNode y){var go=new GameObject(x.id+y.id);go.transform.SetParent(root.transform);var s=go.AddComponent<AccessibilitySegment>();s.id=go.name;s.start=x;s.end=y;graph.segments.Add(s);return s;}
        private RouteComparison Compare()=>RouteComparison.Create(RouteCalculator.Calculate(graph,a,b,profile,false),RouteCalculator.Calculate(graph,a,b,profile,true),profile);
        [Test] public void ComparisonMeasuresDetourAndIdentifiesExcludedShortestPath()
        {
            direct.stepHeight=.16f;var comparison=Compare();Assert.That(comparison.HasBoth,Is.True);
            Assert.That(comparison.ExtraDistance,Is.GreaterThan(5));Assert.That(comparison.ExtraPercent,Is.GreaterThan(50));
            Assert.That(comparison.conventional.blockedCount,Is.EqualTo(1));Assert.That(comparison.accessible.blockedCount,Is.Zero);
            StringAssert.Contains("impedido",comparison.explanation);
        }
        [Test] public void ReverseDirectionSwapsUphillForDownhillAndAccumulatesDescent()
        {
            var analysis=RouteAnalysis.Create(RouteCalculator.Calculate(graph,b,a,profile,true),profile);
            Assert.That(analysis.descent,Is.EqualTo(.5f).Within(.001));Assert.That(analysis.maxUphill,Is.Zero);
            Assert.That(analysis.maxDownhill,Is.EqualTo(5).Within(.001));Assert.That(analysis.segments[0].forward,Is.False);
            Assert.That(analysis.maxCrossSlope,Is.EqualTo(1));Assert.That(analysis.minClearance,Is.EqualTo(2.4f));
        }
        [Test] public void UnknownSegmentsHaveTheirOwnCountAndJustification()
        {
            direct.hasAssessmentData=false;var comparison=Compare();Assert.That(comparison.conventional.unknownCount,Is.EqualTo(1));
            Assert.That(comparison.conventional.blockedCount,Is.Zero);StringAssert.Contains("sem avaliação",comparison.explanation);
        }
        [Test] public void PassableAlternativeCanBeChosenForCostWithoutInventingAnImpediment()
        {
            direct.roughness=.5f;profile.weights.roughness=10;var comparison=Compare();
            Assert.That(comparison.accessible.route.arcs.Count,Is.EqualTo(2));Assert.That(comparison.conventional.blockedCount,Is.Zero);
            StringAssert.Contains("menor custo ponderado",comparison.explanation);
        }
        [Test] public void CoincidentEndpointsProduceFiniteZeroDisplacementSummary()
        {
            var route=RouteCalculator.Calculate(graph,a,a,profile,true);var comparison=RouteComparison.Create(route,route,profile);
            Assert.That(comparison.HasBoth,Is.True);Assert.That(comparison.ExtraPercent,Is.Zero);Assert.That(comparison.accessible.minClearance,Is.Zero);
            Assert.That(comparison.accessible.descent,Is.Zero);Assert.That(comparison.accessible.segments,Is.Empty);
        }
        [TestCase(1024,768)][TestCase(1280,720)][TestCase(1920,1080)][TestCase(2560,1440)][TestCase(900,1200)]
        [TestCase(1600,1000)][TestCase(2560,1080)][TestCase(800,600)]
        public void PanelsToolbarAndViewportRemainSeparatedAtDifferentAspectRatios(int width,int height)
        {
            var layout=new SimulationLayout(width,height);Assert.That(layout.left.Overlaps(layout.right),Is.False);
            Assert.That(layout.toolbar.width,Is.GreaterThan(600));Assert.That(layout.viewport.width,Is.GreaterThan(.4f));
            Assert.That(layout.toolbar.Overlaps(layout.footer),Is.False);
            Assert.That(layout.viewport.xMin*width,Is.GreaterThan(layout.left.xMax*layout.scale));
            Assert.That(layout.viewport.xMax*width,Is.LessThan(layout.right.xMin*layout.scale));
            Assert.That(layout.map.Overlaps(layout.toolbar)||layout.map.Overlaps(layout.footer),Is.False,"O mapa não deve aparecer atrás das barras.");
            Assert.That(layout.map.yMin,Is.GreaterThan(layout.toolbar.yMax));
            Assert.That(layout.map.yMax,Is.LessThan(layout.footer.yMin));
            Assert.That((1-layout.viewport.yMax)*height,Is.EqualTo(layout.map.yMin*layout.scale).Within(.01));
            Assert.That((1-layout.viewport.yMin)*height,Is.EqualTo(layout.map.yMax*layout.scale).Within(.01));
        }
    }
}
