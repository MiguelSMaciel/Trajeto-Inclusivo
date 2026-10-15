using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrajetoInclusivo.Editor
{
    public sealed class GeoImportWindow : EditorWindow
    {
        private TextAsset source;
        private GeographicReference reference = new GeographicReference();
        private string report = "Importe somente dados cuja origem e licença você tenha registrado.";
        [MenuItem("Trajeto Inclusivo/Inspecionar GeoJSON local")]
        private static void Open() => GetWindow<GeoImportWindow>("Importar GeoJSON");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Prévia geométrica. Importar vias não cria automaticamente uma rede de calçadas avaliada. A origem precisa ser informada; não há coordenadas presumidas de Vitória.", MessageType.Info);
            source = (TextAsset)EditorGUILayout.ObjectField("GeoJSON (.json)", source, typeof(TextAsset), false);
            reference.coordinates = (GeoCoordinateSystem)EditorGUILayout.EnumPopup("Coordenadas", reference.coordinates);
            if (reference.coordinates == GeoCoordinateSystem.Wgs84LongitudeLatitude)
            {
                reference.originLongitude = EditorGUILayout.DoubleField("Longitude da origem", reference.originLongitude);
                reference.originLatitude = EditorGUILayout.DoubleField("Latitude da origem", reference.originLatitude);
            }
            else
            {
                reference.originEasting = EditorGUILayout.DoubleField("Origem leste (m)", reference.originEasting);
                reference.originNorthing = EditorGUILayout.DoubleField("Origem norte (m)", reference.originNorthing);
            }
            reference.originAltitude = EditorGUILayout.DoubleField("Altitude da origem (m)", reference.originAltitude);
            if (GUILayout.Button("Validar arquivo e coordenadas locais") && source != null)
            {
                try
                {
                    var features = GeoDataImporter.Import(source.text, reference);
                    int points = 0, missing = 0;
                    var bounds = new Bounds(); bool first = true;
                    foreach (var f in features)
                    {
                        if (!f.hasElevation) missing++;
                        foreach (var part in f.parts) foreach (var p in part) { points++; if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p); }
                    }
                    report = $"{features.Count} feições; {points} pontos; {missing} feições sem cota completa.\nLimites locais: {bounds}.\nDados importados não verificados; nenhuma calçada ou acessibilidade foi inferida.";
                    Debug.Log(report);
                }
                catch (System.Exception e) { report = e.Message; }
            }
            EditorGUILayout.HelpBox(report, MessageType.None);
        }
    }
}
