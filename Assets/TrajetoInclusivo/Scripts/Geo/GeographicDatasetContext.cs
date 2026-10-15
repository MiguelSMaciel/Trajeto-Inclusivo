using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class GeographicDatasetContext : MonoBehaviour
    {
        public GeographicMapDefinition definition;
        public int roadFeatureCount, buildingFeatureCount, omittedPolygonCount;
        public bool useSimulatedAssessment;
        public Bounds mapBounds;
        public PedestrianNode plaza,ifes,faesa;
        public void ApplyAssessmentMode(PedestrianGraph graph)
        {
            foreach (var segment in graph.segments)
            {
                segment.hasAssessmentData = useSimulatedAssessment;
                // Geometry follows mapped axes, but offset sidewalks and all assessment attributes are artificial.
                segment.verification = VerificationState.Simulated;
            }
        }
        public void SetAssessmentMode(bool enabled, SimulationController simulation)
        {
            useSimulatedAssessment = enabled; ApplyAssessmentMode(simulation.graph);
            simulation.RequestRecalculation();
        }
    }
}
