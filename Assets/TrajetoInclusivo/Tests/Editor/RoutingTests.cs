using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TrajetoInclusivo.Tests
{
    public sealed class RoutingTests
    {
        private GameObject root;
        private PedestrianGraph graph;
        private AccessibilityProfile profile;
        private PedestrianNode a, b, c;
        private AccessibilitySegment direct;
        [SetUp] public void Setup()
        {
            root = new GameObject("Test"); graph = root.AddComponent<PedestrianGraph>();
            profile = ScriptableObject.CreateInstance<AccessibilityProfile>();
            a = Node("A", Vector3.zero); b = Node("B", new Vector3(10, 0, 0)); c = Node("C", new Vector3(5, 0, 6));
            direct = Edge(a, b); Edge(a, c); Edge(c, b); graph.Rebuild();
        }
        [TearDown] public void Teardown() { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        private PedestrianNode Node(string id, Vector3 position)
        {
            var go = new GameObject(id); go.transform.SetParent(root.transform); go.transform.position = position;
            var n = go.AddComponent<PedestrianNode>(); n.id = id; graph.nodes.Add(n); return n;
        }
        private AccessibilitySegment Edge(PedestrianNode from, PedestrianNode to)
        {
            var go = new GameObject(from.id + "-" + to.id); go.transform.SetParent(root.transform);
            var s = go.AddComponent<AccessibilitySegment>(); s.start = from; s.end = to; s.id = go.name;
            graph.segments.Add(s); return s;
        }
        private RouteResult Route(bool accessible = true) => RouteCalculator.Calculate(graph, a, b, profile, accessible);
        [Test] public void ConventionalUsesKnownShortestDistance() { Assert.That(Route(false).distance, Is.EqualTo(10).Within(.001)); }
        [Test] public void AccessibleUsesDirectPathWithoutBarriers() { Assert.That(Route().arcs[0].segment, Is.SameAs(direct)); }
        [Test] public void ImpassableStepEliminatesEdgeAndUsesDetour()
        {
            direct.stepHeight = .16f;
            Assert.That(Route().distance, Is.GreaterThan(15)); Assert.That(Route(false).distance, Is.EqualTo(10));
            Assert.That(Route().arcs.Exists(arc => arc.segment == direct), Is.False);
        }
        [Test] public void PhysicalClosureAffectsBothModes()
        {
            direct.networkOpen = false; Assert.That(Route().arcs.Count, Is.EqualTo(2)); Assert.That(Route(false).arcs.Count, Is.EqualTo(2));
        }
        [Test] public void WidthAndPotholeForceDetour()
        {
            var o = direct.gameObject.AddComponent<AccessibilityObstacle>(); o.Attach(direct);
            o.kind = ObstacleKind.Pothole; o.remainingFreeWidth = .65f;
            Assert.That(Route().arcs.Count, Is.EqualTo(2));
        }
        [Test] public void MissingDataIsNeverAutomaticallyAccessible()
        {
            direct.hasAssessmentData = false;
            Assert.That(AccessibilityEvaluator.Evaluate(direct, profile, true).rating, Is.EqualTo(AccessibilityRating.NotEvaluated));
            Assert.That(Route().arcs.Count, Is.EqualTo(2));
            foreach (var s in graph.segments) s.hasAssessmentData = false;
            Assert.That(Route().failure, Is.EqualTo(RouteFailure.InsufficientData));
        }
        [Test] public void DiagnosesDisconnectedTopologySeparately()
        {
            graph.segments.Clear(); graph.Rebuild(); Assert.That(Route().failure, Is.EqualTo(RouteFailure.NoConnection));
        }
        [Test] public void DiagnosesBlockedNetwork()
        {
            foreach (var s in graph.segments) s.networkOpen = false;
            Assert.That(Route().failure, Is.EqualTo(RouteFailure.Blocked));
        }
        [Test] public void SameOriginAndDestinationIsZeroLengthRoute()
        {
            var result = RouteCalculator.Calculate(graph, a, a, profile, true);
            Assert.That(result.Found, Is.True); Assert.That(result.distance, Is.Zero);
        }
        [Test] public void UphillAndDownhillAreEvaluatedIndependently()
        {
            b.transform.position = new Vector3(10, 1.2f, 0);
            profile.maxUphillPercent = 8; profile.maxDownhillPercent = 15;
            Assert.That(direct.SlopePercent, Is.EqualTo(12).Within(.001));
            Assert.That(AccessibilityEvaluator.Evaluate(direct, profile, true).Passable, Is.False);
            Assert.That(AccessibilityEvaluator.Evaluate(direct, profile, false).Passable, Is.True);
        }
        [Test] public void CrossSlopeCanBlockOtherwiseFlatRoute()
        { direct.crossSlopePercent = 6; Assert.That(Route().arcs.Count, Is.EqualTo(2)); }
        [Test] public void WeightedCostMayChooseLongerPassablePath()
        {
            direct.roughness = .5f; profile.weights.roughness = 10;
            Assert.That(Route().arcs.Count, Is.EqualTo(2)); Assert.That(Route(false).arcs.Count, Is.EqualTo(1));
        }
        [Test] public void RestoringWidthRestoresDirectRoute()
        {
            direct.freeWidth = .6f; Assert.That(Route().arcs.Count, Is.EqualTo(2));
            direct.freeWidth = 2.4f; Assert.That(Route().arcs.Count, Is.EqualTo(1));
        }
        [Test] public void RequiredWidthIsIndividualAndReferenceWidthIsOnlyDifficulty()
        {
            direct.freeWidth = 1.0f;
            var evaluation = AccessibilityEvaluator.Evaluate(direct, profile, true);
            Assert.That(evaluation.Passable, Is.True); Assert.That(evaluation.rating, Is.EqualTo(AccessibilityRating.Difficult));
        }
        [Test] public void OneWayGraphRespectsDirection()
        {
            foreach (var s in graph.segments) s.bidirectional = false;
            graph.Rebuild(); Assert.That(RouteCalculator.Calculate(graph, b, a, profile, false).Found, Is.False);
        }
        [Test] public void AscentAndSlopeUseGeometry()
        {
            b.transform.position = new Vector3(10, .5f, 0);
            var result = Route(false);
            Assert.That(result.ascent, Is.EqualTo(.5).Within(.001)); Assert.That(result.maxSlope, Is.EqualTo(5).Within(.001));
            Assert.That(result.distance, Is.EqualTo(Mathf.Sqrt(100.25f)).Within(.001));
        }
    }
}
