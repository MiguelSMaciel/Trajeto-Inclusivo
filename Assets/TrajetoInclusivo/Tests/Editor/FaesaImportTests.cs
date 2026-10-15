using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;

namespace TrajetoInclusivo.Tests
{
    public sealed class FaesaImportTests
    {
        private const string Folder="Assets/TrajetoInclusivo/Data/Geography/FAESA/";
        [Test] public void ExpandedSnapshotIncludesTheMappedAddressAndKeepsTheSameLocalOrigin()
        {
            var definition=AssetDatabase.LoadAssetAtPath<GeographicMapDefinition>(Folder+"AvenidaVitoria.asset");
            var original=AssetDatabase.LoadAssetAtPath<GeographicMapDefinition>("Assets/TrajetoInclusivo/Data/Geography/AvenidaVitoria.asset");
            Assert.That(definition.reference.originLongitude,Is.EqualTo(original.reference.originLongitude));
            Assert.That(definition.reference.originLatitude,Is.EqualTo(original.reference.originLatitude));
            var features=GeoDataImporter.Import(definition.geoJson.text,definition.reference);
            var faesa=features.Single(f=>f.id==definition.faesaReferenceId);
            Assert.That(faesa.Property("addr:housenumber"),Is.EqualTo("2220"));Assert.That(faesa.Property("addr:street"),Is.EqualTo("Avenida Vitória"));
            Assert.That(faesa.geometryType,Is.EqualTo("Polygon"));Assert.That(features.All(f=>!f.hasElevation),Is.True);
            Assert.That(features.Count(f=>f.Property("kind")=="building"),Is.EqualTo(1212));
            Assert.That(features.Any(f=>f.id==definition.plazaId),Is.True);Assert.That(features.Any(f=>f.id==definition.campusGateId),Is.True);
            Assert.That(definition.provenance.text,Does.Contain("https://www.faesa.br/contato/"));
        }
        [Test] public void ExpandedSourceChecksumMatchesItsOwnProvenance()
        {
            using var hash=SHA256.Create();var digest=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(Folder+"avenida_vitoria_faesa.osm"))).Replace("-","").ToLowerInvariant();
            Assert.That(File.ReadAllText(Folder+"provenance.json"),Does.Contain(digest));
        }
    }
}
