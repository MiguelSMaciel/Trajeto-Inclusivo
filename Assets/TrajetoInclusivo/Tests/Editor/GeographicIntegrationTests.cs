using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TrajetoInclusivo.Tests
{
    public sealed class GeographicIntegrationTests
    {
        [Test] public void SnapshotPreservesLandmarkNamesSourceIdsAndMissingElevations()
        {
            var definition = AssetDatabase.LoadAssetAtPath<GeographicMapDefinition>("Assets/TrajetoInclusivo/Data/Geography/AvenidaVitoria.asset");
            Assert.That(definition, Is.Not.Null);
            var features = GeoDataImporter.Import(definition.geoJson.text, definition.reference);
            Assert.That(features.Count(f => f.Property("kind") == "building"), Is.EqualTo(787));
            Assert.That(features.First(f => f.id == definition.plazaId).name, Is.EqualTo("Praça Asdrúbal Soares"));
            Assert.That(features.First(f => f.id == definition.campusId).Property("addr:housenumber"), Is.EqualTo("1729"));
            Assert.That(features.Any(f => f.name == "Avenida Vitória"), Is.True);
            Assert.That(features.All(f => !f.hasElevation), Is.True);
            Assert.That(features.All(f => f.parts.All(part => part.All(p => Mathf.Abs(p.x) < 1000 && Mathf.Abs(p.z) < 1000))), Is.True);
        }
        [Test] public void SourceChecksumMatchesProvenance()
        {
            const string folder = "Assets/TrajetoInclusivo/Data/Geography/";
            using var hash = SHA256.Create();
            string digest = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(folder + "jucutuquara.osm"))).Replace("-", "").ToLowerInvariant();
            Assert.That(File.ReadAllText(folder + "provenance.json"), Does.Contain(digest));
        }
        [Test] public void ConcaveFootprintTriangulationPreservesAreaAndUpwardNormals()
        {
            var builder = new GeographicMeshBuilder();
            Assert.That(builder.Polygon(new[] {P(0,0),P(4,0),P(4,4),P(2,2),P(0,4),P(0,0)},0,0), Is.True);
            var mesh = builder.ToMesh("test");
            float area = 0; var v = mesh.vertices; var t = mesh.triangles;
            for (int i = 0; i < t.Length; i+=3) area += Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]).magnitude*.5f;
            Assert.That(area, Is.EqualTo(12).Within(.001)); Assert.That(mesh.normals.All(n => n.y > .99f), Is.True);
            Object.DestroyImmediate(mesh);
        }
        [Test] public void RoadRibbonFacesUp()
        {
            var builder = new GeographicMeshBuilder(); builder.Road(P(0,0),P(20,0),6);
            var mesh = builder.ToMesh("road"); Assert.That(mesh.normals.All(n => n.y > .99f), Is.True); Object.DestroyImmediate(mesh);
        }
        [TestCase(false, false, false)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void ConnectivityUsesSourceNodeIdsAndLayers(bool sharedJunction, bool separateLayers, bool expectedConnection)
        {
            var features = new List<ImportedGeoFeature>();
            if (sharedJunction)
            {
                features.Add(Road("h0",P(-30,0),P(0,0),"west,center","0")); features.Add(Road("h1",P(0,0),P(30,0),"center,east","0"));
                features.Add(Road("v0",P(0,-30),P(0,0),"south,center",separateLayers?"1":"0")); features.Add(Road("v1",P(0,0),P(0,30),"center,north",separateLayers?"1":"0"));
            }
            else
            {
                features.Add(Road("h",P(-30,0),P(30,0),"west,east","0"));
                features.Add(Road("v",P(0,-30),P(0,30),"south,north","0"));
            }
            AddLandmarks(features);
            var root = new GameObject("Fixture"); var graph = root.AddComponent<PedestrianGraph>();
            var definition = ScriptableObject.CreateInstance<GeographicMapDefinition>(); definition.plazaId="plaza"; definition.campusGateId="gate";
            var profile = ScriptableObject.CreateInstance<AccessibilityProfile>();
            try
            {
                GeographicNetworkBuilder.Build(features,definition,graph,null,out var a,out var b);
                var route = RouteCalculator.Calculate(graph,a,b,profile,false);
                Assert.That(route.Found,Is.EqualTo(expectedConnection));
                Assert.That(graph.segments.All(s=>!s.hasAssessmentData),Is.True);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(definition); Object.DestroyImmediate(profile); }
        }
        [Test] public void ExplicitRightSidewalkUsesRightOfOsmWayDirection()
        {
            var f=Road("h",P(-30,0),P(30,0),"west,east","0"); f.properties["sidewalk"]="right";
            var features=new List<ImportedGeoFeature>{f}; AddLandmarks(features);
            // Keep the gate near the only road in this fixture.
            features.First(x=>x.id=="gate").parts[0][0]=P(20,0);
            var root=new GameObject("Right sidewalk"); var graph=root.AddComponent<PedestrianGraph>();
            var definition=ScriptableObject.CreateInstance<GeographicMapDefinition>(); definition.plazaId="plaza"; definition.campusGateId="gate";
            try
            {
                GeographicNetworkBuilder.Build(features,definition,graph,null,out _,out _);
                Assert.That(graph.segments.Where(s=>!s.syntheticConnection).All(s=>s.start.Position.z<0 && s.end.Position.z<0),Is.True);
            }
            finally {Object.DestroyImmediate(root);Object.DestroyImmediate(definition);}
        }
        private static Vector3 P(float x,float z)=>new Vector3(x,0,z);
        private static ImportedGeoFeature Road(string id,Vector3 a,Vector3 b,string refs,string layer)
        {
            var f=new ImportedGeoFeature{id=id,name="Rua de teste",geometryType="LineString"};f.parts.Add(new[]{a,b});
            f.properties["kind"]="road";f.properties["highway"]="residential";f.properties["osm_id"]="way/"+id;f.properties["node_refs"]=refs;f.properties["layer"]=layer;return f;
        }
        private static void AddLandmarks(List<ImportedGeoFeature> features)
        {
            var p=new ImportedGeoFeature{id="plaza",geometryType="Polygon"};p.parts.Add(new[]{P(-21,-1),P(-19,-1),P(-19,1),P(-21,1),P(-21,-1)});features.Add(p);
            var gate=new ImportedGeoFeature{id="gate",geometryType="Point"};gate.parts.Add(new[]{P(0,20)});features.Add(gate);
        }
    }
}
