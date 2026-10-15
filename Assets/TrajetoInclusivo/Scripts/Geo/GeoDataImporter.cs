using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace TrajetoInclusivo
{
    public enum GeoCoordinateSystem { Wgs84LongitudeLatitude, Sirgas2000Utm24South }

    [Serializable]
    public sealed class GeographicReference
    {
        public GeoCoordinateSystem coordinates;
        public double originLongitude;
        public double originLatitude;
        public double originEasting;
        public double originNorthing;
        public double originAltitude;
    }

    public sealed class ImportedGeoFeature
    {
        public string id, name, geometryType;
        public readonly List<Vector3[]> parts = new List<Vector3[]>();
        public readonly Dictionary<string, string> properties = new Dictionary<string, string>();
        public bool hasElevation = true;
        public VerificationState verification = VerificationState.ImportedUnverified;
        public string Property(string key) => properties.TryGetValue(key, out var value) ? value : "";
    }

    public static class GeoDataImporter
    {
        // Geometry import only: a road centerline is never silently treated as a verified sidewalk network.
        // GeoJSON altitude units/datum must be checked by the dataset author.
        public static List<ImportedGeoFeature> Import(string json, GeographicReference reference)
        {
            var root = MiniJson.Read(json) as Dictionary<string, object>;
            if (root == null || String(root, "type") != "FeatureCollection") throw new FormatException("Esperado GeoJSON FeatureCollection.");
            if (root.TryGetValue("crs", out var crs))
            {
                var label = crs as Dictionary<string, object>;
                var properties = label != null && label.TryGetValue("properties", out var p) ? p as Dictionary<string, object> : null;
                string name = properties == null ? "" : String(properties, "name");
                string expected = reference.coordinates == GeoCoordinateSystem.Sirgas2000Utm24South ? "31984" : "4326";
                if (!name.Contains(expected) && !(expected == "4326" && name.Contains("CRS84")))
                    throw new FormatException("CRS declarado não corresponde à referência selecionada: " + name);
            }
            if (!(root["features"] is List<object> features)) throw new FormatException("Lista de feições ausente.");
            var result = new List<ImportedGeoFeature>();
            foreach (var item in features)
            {
                var f = item as Dictionary<string, object>;
                if (f == null || !f.TryGetValue("geometry", out var geometry) || !(geometry is Dictionary<string, object> g)) continue;
                var props = f.TryGetValue("properties", out var prop) ? prop as Dictionary<string, object> : null;
                var feature = new ImportedGeoFeature { id = String(f, "id"), name = props == null ? "" : String(props, "name"), geometryType = String(g, "type") };
                if (props != null) foreach (var property in props)
                    if (property.Value == null || property.Value is string || property.Value is double || property.Value is bool)
                        feature.properties[property.Key] = Convert.ToString(property.Value, CultureInfo.InvariantCulture);
                if (!g.TryGetValue("coordinates", out var coords)) throw new FormatException("Geometria sem coordenadas.");
                CollectCoordinates(coords, feature, reference);
                result.Add(feature);
            }
            return result;
        }
        private static void CollectCoordinates(object value, ImportedGeoFeature feature, GeographicReference reference)
        {
            if (!(value is List<object> list) || list.Count == 0) return;
            if (list[0] is double)
            {
                feature.parts.Add(new[] { Coordinate(list, feature, reference) }); return;
            }
            if (list[0] is List<object> first && first.Count > 0 && first[0] is double)
            {
                var points = new Vector3[list.Count];
                for (int i = 0; i < list.Count; i++) points[i] = Coordinate((List<object>)list[i], feature, reference);
                feature.parts.Add(points); return;
            }
            foreach (var part in list) CollectCoordinates(part, feature, reference);
        }
        private static Vector3 Coordinate(List<object> coordinate, ImportedGeoFeature feature, GeographicReference r)
        {
            if (coordinate.Count < 2) throw new FormatException("Coordenada deve ter dois ou três números.");
            double x = (double)coordinate[0], z = (double)coordinate[1];
            double originX = r.originEasting, originZ = r.originNorthing;
            if (r.coordinates == GeoCoordinateSystem.Wgs84LongitudeLatitude)
            {
                (x, z) = ProjectUtm24South(x, z);
                (originX, originZ) = ProjectUtm24South(r.originLongitude, r.originLatitude);
            }
            double altitude = 0;
            if (coordinate.Count >= 3) altitude = (double)coordinate[2] - r.originAltitude;
            else feature.hasElevation = false;
            // All global arithmetic remains double; cast only AFTER subtracting the local origin.
            return new Vector3((float)(x - originX), (float)altitude, (float)(z - originZ));
        }
        public static (double easting, double northing) ProjectUtm24South(double longitude, double latitude)
        {
            if (longitude < -42 || longitude > -36 || latitude < -80 || latitude > 0)
                throw new ArgumentOutOfRangeException("Coordenada fora da zona UTM 24S. Reprojete o arquivo no QGIS.");
            const double a = 6378137, flattening = 1 / 298.257222101, k = .9996;
            double e2 = flattening * (2 - flattening), ep2 = e2 / (1 - e2);
            double lat = latitude * Math.PI / 180, lon = longitude * Math.PI / 180;
            double sin = Math.Sin(lat), cos = Math.Cos(lat), tan = Math.Tan(lat);
            double n = a / Math.Sqrt(1 - e2 * sin * sin), t = tan * tan, c = ep2 * cos * cos;
            double aa = cos * (lon - (-39 * Math.PI / 180));
            double m = a * ((1-e2/4-3*e2*e2/64-5*e2*e2*e2/256)*lat
                -(3*e2/8+3*e2*e2/32+45*e2*e2*e2/1024)*Math.Sin(2*lat)
                +(15*e2*e2/256+45*e2*e2*e2/1024)*Math.Sin(4*lat)
                -35*e2*e2*e2/3072*Math.Sin(6*lat));
            double east = 500000 + k*n*(aa+(1-t+c)*Math.Pow(aa,3)/6+(5-18*t+t*t+72*c-58*ep2)*Math.Pow(aa,5)/120);
            double north = 10000000 + k*(m+n*tan*(aa*aa/2+(5-t+9*c+4*c*c)*Math.Pow(aa,4)/24+(61-58*t+t*t+600*c-330*ep2)*Math.Pow(aa,6)/720));
            return (east, north);
        }
        private static string String(Dictionary<string, object> dict, string key) => dict.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : "";
    }

    // Small strict JSON reader, with no package/network dependency. No eval or code execution.
    internal sealed class MiniJson
    {
        private readonly string text;
        private int position;
        private MiniJson(string text) { this.text = text; }
        public static object Read(string text)
        {
            var reader = new MiniJson(text); var result = reader.Value(0); reader.White();
            if (reader.position != text.Length) throw new FormatException("Conteúdo após JSON.");
            return result;
        }
        private void White() { while (position < text.Length && char.IsWhiteSpace(text[position])) position++; }
        private object Value(int depth)
        {
            if (depth > 64) throw new FormatException("JSON muito profundo.");
            White(); if (position >= text.Length) throw new FormatException("JSON incompleto.");
            char ch = text[position++];
            if (ch == '"') return Quoted();
            if (ch == '{')
            {
                var dict = new Dictionary<string, object>(); White();
                if (Take('}')) return dict;
                do { White(); Need('"'); string key = Quoted(); White(); Need(':'); dict[key] = Value(depth+1); White(); }
                while (Take(','));
                Need('}'); return dict;
            }
            if (ch == '[')
            {
                var list = new List<object>(); White(); if (Take(']')) return list;
                do { list.Add(Value(depth+1)); White(); } while (Take(','));
                Need(']'); return list;
            }
            position--;
            foreach (var literal in new[] { "true", "false", "null" })
                if (position + literal.Length <= text.Length && string.CompareOrdinal(text, position, literal, 0, literal.Length) == 0)
                { position += literal.Length; return literal == "null" ? null : (object)(literal == "true"); }
            int start = position;
            while (position < text.Length && "0123456789+-.eE".IndexOf(text[position]) >= 0) position++;
            if (start == position || !double.TryParse(text.Substring(start, position-start), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || double.IsInfinity(number) || double.IsNaN(number)) throw new FormatException("Número inválido em JSON.");
            return number;
        }
        private string Quoted()
        {
            var result = new StringBuilder();
            while (position < text.Length)
            {
                char c = text[position++]; if (c == '"') return result.ToString();
                if (c < 32) throw new FormatException("Controle inválido em string JSON.");
                if (c != '\\') { result.Append(c); continue; }
                if (position >= text.Length) break;
                c = text[position++];
                switch (c)
                {
                    case '"': case '\\': case '/': result.Append(c); break;
                    case 'n': result.Append('\n'); break; case 'r': result.Append('\r'); break;
                    case 't': result.Append('\t'); break; case 'b': result.Append('\b'); break; case 'f': result.Append('\f'); break;
                    case 'u':
                        if (position + 4 > text.Length) throw new FormatException("Escape Unicode incompleto.");
                        result.Append((char)int.Parse(text.Substring(position, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); position += 4; break;
                    default: throw new FormatException("Escape JSON inválido.");
                }
            }
            throw new FormatException("String JSON incompleta.");
        }
        private bool Take(char c) { if (position < text.Length && text[position] == c) { position++; return true; } return false; }
        private void Need(char c) { if (!Take(c)) throw new FormatException("Esperado '" + c + "' no JSON."); }
    }
}
