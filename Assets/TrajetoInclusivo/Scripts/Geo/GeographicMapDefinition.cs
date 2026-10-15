using UnityEngine;

namespace TrajetoInclusivo
{
    [CreateAssetMenu(menuName = "Trajeto Inclusivo/Mapa geográfico")]
    public sealed class GeographicMapDefinition : ScriptableObject
    {
        public TextAsset geoJson;
        public TextAsset provenance;
        public GeographicReference reference = new GeographicReference();
        public string datasetDate = "2026-10-15";
        public string attribution = "© OpenStreetMap contributors · ODbL 1.0";
        [Min(.6f)] public float simulatedSidewalkWidth = 2.4f;
        [Min(1)] public float assumedLaneWidth = 3.2f;
        [Min(1)] public float assumedLocalRoadWidth = 6;
        [Min(0)] public float simulatedSidewalkElevation = .18f;
        [Min(1)] public float assumedBuildingHeight = 6;
        public string plazaId = "way/541652953";
        public string campusId = "way/1352259307";
        public string campusGateId = "node/12511661225";
        public string faesaReferenceId;
    }
}
