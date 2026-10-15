using NUnit.Framework;

namespace TrajetoInclusivo.Tests
{
    public sealed class GeoImportTests
    {
        [Test] public void UtmGlobalCoordinatesBecomeLocalMetresBeforeFloatConversion()
        {
            const string json = "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"properties\":{\"name\":\"Rua de teste\"},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[[500000,7700000,12],[500010.25,7700004.5,13]]}}]}";
            var reference = new GeographicReference { coordinates = GeoCoordinateSystem.Sirgas2000Utm24South, originEasting = 500000, originNorthing = 7700000, originAltitude = 12 };
            var features = GeoDataImporter.Import(json, reference);
            Assert.That(features[0].name, Is.EqualTo("Rua de teste"));
            Assert.That(features[0].parts[0][1].x, Is.EqualTo(10.25));
            Assert.That(features[0].parts[0][1].z, Is.EqualTo(4.5)); Assert.That(features[0].parts[0][1].y, Is.EqualTo(1));
        }
        [Test] public void MissingElevationRemainsExplicitlyUnknown()
        {
            const string json = "{\"type\":\"FeatureCollection\",\"features\":[{\"geometry\":{\"type\":\"Point\",\"coordinates\":[500000,7700000]}}]}";
            var result = GeoDataImporter.Import(json, new GeographicReference { coordinates = GeoCoordinateSystem.Sirgas2000Utm24South });
            Assert.That(result[0].hasElevation, Is.False);
        }
        [Test] public void UtmProjectionHasKnownCentralMeridianFalseOrigin()
        {
            var p = GeoDataImporter.ProjectUtm24South(-39, 0);
            Assert.That(p.easting, Is.EqualTo(500000).Within(.001)); Assert.That(p.northing, Is.EqualTo(10000000).Within(.001));
        }
        [Test] public void RejectsCoordinatesOutsideZone() => Assert.Throws<System.ArgumentOutOfRangeException>(() => GeoDataImporter.ProjectUtm24South(0, 20));
        [Test] public void RejectsMalformedJson() => Assert.Throws<System.FormatException>(() => GeoDataImporter.Import("{", new GeographicReference()));
    }
}
