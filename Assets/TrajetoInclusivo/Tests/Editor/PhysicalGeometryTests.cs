using System;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace TrajetoInclusivo.Tests
{
    public sealed class PhysicalGeometryTests
    {
        private GameObject root;
        private AccessibilitySegment s;
        [SetUp] public void Setup()
        {
            root=new GameObject("Physical test");s=root.AddComponent<AccessibilitySegment>();s.totalWidth=s.freeWidth=2.4f;s.crossSlopePercent=0;
            s.start=Node("A",Vector3.zero);s.end=Node("B",new Vector3(10,0,0));
        }
        private PedestrianNode Node(string id,Vector3 p){var go=new GameObject(id);go.transform.SetParent(root.transform);go.transform.position=p;var n=go.AddComponent<PedestrianNode>();n.id=id;return n;}
        private AccessibilityObstacle Obstacle(float width,float lateral,float position=.5f,ObstacleKind kind=ObstacleKind.Generic)
        {
            var go=new GameObject("Obstacle");go.transform.SetParent(root.transform);var o=go.AddComponent<AccessibilityObstacle>();o.Attach(s);
            o.useFootprintForClearance=true;o.kind=kind;o.occupiedWidth=width;o.lateralOffset=lateral;o.positionAlongSegment=position;o.length=2;return o;
        }
        [TearDown] public void Teardown()=>Object.DestroyImmediate(root);
        [Test] public void CentralObstacleLeavesTwoSeparateGaps()
        {Obstacle(.8f,0);Assert.That(s.EffectiveWidth,Is.EqualTo(.8f).Within(.0001));}
        [Test] public void OverlappingLongitudinalFootprintsCombineTheirOccupiedIntervals()
        {Obstacle(.8f,-.8f);Obstacle(.8f,.8f);Assert.That(s.EffectiveWidth,Is.EqualTo(.8f).Within(.0001));}
        [Test] public void LongitudinallySeparatedObstaclesAreNotAddedAcrossTheWholeSegment()
        {Obstacle(.8f,-.8f,.2f);Obstacle(.8f,.8f,.8f);Assert.That(s.EffectiveWidth,Is.EqualTo(1.6f).Within(.0001));}
        [Test] public void PartialStepMayBeBypassedButFullWidthStepBlocks()
        {
            var p=ScriptableObject.CreateInstance<AccessibilityProfile>();var o=Obstacle(.8f,.8f,.5f,ObstacleKind.Step);o.heightOrDepth=.16f;
            try
            {
                Assert.That(AccessibilityEvaluator.Evaluate(s,p,true).Passable,Is.True);
                o.occupiedWidth=s.totalWidth;Assert.That(AccessibilityEvaluator.Evaluate(s,p,true).Passable,Is.False);
                p.maxStepHeight=.2f;Assert.That(AccessibilityEvaluator.Evaluate(s,p,true).Passable,Is.True);
            }finally{Object.DestroyImmediate(p);}
        }
        [Test] public void PotholeColliderHasAFloorBelowTheSlopingPavement()
        {
            s.end.transform.position=new Vector3(10,1,0);s.crossSlopePercent=2;
            var o=Obstacle(1,0,.5f,ObstacleKind.Pothole);o.heightOrDepth=.2f;
            var mesh=SidewalkSurfaceBuilder.Build(s,.18f,root.transform);var collider=root.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
            try
            {
                Physics.SyncTransforms();
                Assert.That(collider.Raycast(new Ray(new Vector3(5,5,0),Vector3.down),out var hit,10),Is.True);
                Assert.That(hit.point.y,Is.EqualTo(.3f).Within(.0001),"O collider deve atingir o fundo, sem uma tampa plana invisível.");
                Assert.That(collider.Raycast(new Ray(new Vector3(2,5,0),Vector3.down),out hit,10),Is.True);
                Assert.That(hit.point.y,Is.EqualTo(.2f).Within(.0001));
            }finally{collider.sharedMesh=null;Object.DestroyImmediate(mesh);}
        }
        [Test] public void StepHeightIsMeasuredVerticallyAboveTheSlopingSurface()
        {
            s.end.transform.position=new Vector3(10,1.2f,0);s.crossSlopePercent=-3;
            var o=Obstacle(2.4f,0,.5f,ObstacleKind.Step);o.heightOrDepth=.16f;
            var mesh=SidewalkSurfaceBuilder.BuildRaisedPatch(o,root.transform);
            try
            {
                foreach(var p in mesh.vertices)
                {
                    var local=ObstacleFootprints.LocalPoint(s,p);float offset=p.y-ObstacleFootprints.SurfacePoint(s,local.x,local.y).y;
                    Assert.That(Mathf.Min(Mathf.Abs(offset),Mathf.Abs(offset-.16f)),Is.LessThan(.0001));
                }
            }finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void OverlappingPotholesUseTheDeepestFloor()
        {
            Obstacle(1,0,.5f,ObstacleKind.Pothole).heightOrDepth=.1f;Obstacle(.5f,0,.5f,ObstacleKind.Pothole).heightOrDepth=.25f;
            var mesh=SidewalkSurfaceBuilder.Build(s,.18f,root.transform);var collider=root.AddComponent<MeshCollider>();collider.sharedMesh=mesh;
            try{Physics.SyncTransforms();Assert.That(collider.Raycast(new Ray(new Vector3(5,5,0),Vector3.down),out var hit,10),Is.True);Assert.That(hit.point.y,Is.EqualTo(-.25f).Within(.0001));}
            finally{collider.sharedMesh=null;Object.DestroyImmediate(mesh);}
        }
        [Test] public void PhysicalWavesChangeMeshAndAssessmentWithoutMovingEndpoints()
        {
            s.surfaceAmplitude=.03f;s.surfacePeriod=2;
            var mesh=SidewalkSurfaceBuilder.Build(s,.18f,root.transform);
            try
            {
                Assert.That(mesh.vertexCount,Is.GreaterThan(24));Assert.That(mesh.bounds.max.y,Is.GreaterThan(.02f));
                Assert.That(s.EffectiveRoughness,Is.EqualTo(.6f).Within(.001));
                Assert.That(ObstacleFootprints.SurfacePoint(s,0,0).y,Is.Zero.Within(.0001));
                Assert.That(ObstacleFootprints.SurfacePoint(s,10,0).y,Is.Zero.Within(.0001));
            }finally{Object.DestroyImmediate(mesh);}
        }
        [Test] public void ElevationCsvInterpolatesSubtractsVerticalOriginAndDoesNotExtrapolate()
        {
            var data=ElevationDataImporter.ParseCsv("x,z,elevation\n0,0,100\n10,0,102\n0,10,104\n10,10,106","Test source","Test datum",100);
            try
            {
                ElevationDataImporter.Validate(data);Assert.That(data.verification,Is.EqualTo(VerificationState.ImportedUnverified));
                Assert.That(data.TrySample(5,5,out var y),Is.True);Assert.That(y,Is.EqualTo(3).Within(.0001));
                Assert.That(data.TrySample(10,10,out y),Is.True);Assert.That(y,Is.EqualTo(6));
                Assert.That(data.TrySample(10.1f,5,out _),Is.False);
            }finally{Object.DestroyImmediate(data);}
        }
        [TestCase("x,z,elevation\n0,0,0\n10,0,1\n0,10,2\n0,10,3")]
        [TestCase("x,z,elevation\n0,0,0\n10,0,NaN\n0,10,2\n10,10,3")]
        [TestCase("x,z,elevation\n0,0,0\n10,0,1\n0,10,2\n20,10,3")]
        [TestCase("x,z,elevation\n0,0,0\n10,0,1\n0,10,2\n10,10,3\n21,0,0\n21,10,0")]
        public void InvalidElevationGridsAreRejected(string csv)
        {Assert.Throws<FormatException>(()=>ElevationDataImporter.ParseCsv(csv,"Test","Local"));}
    }
}
