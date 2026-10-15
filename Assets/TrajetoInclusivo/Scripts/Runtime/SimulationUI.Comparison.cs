using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed partial class SimulationUI
    {
        private bool showProfiles,expandConventional,expandAccessible;
        private RouteComparison profileCacheSource;
        private RouteComparison[] profileComparisons;
        public void ShowProfileComparison(bool show)
        {showProfiles=show;detailScroll=Vector2.zero;simulation.selectedSegment=null;}
        public RouteComparison[] GetProfileComparisons()
        {
            if(profileCacheSource==simulation.comparison && profileComparisons!=null)return profileComparisons;
            profileCacheSource=simulation.comparison;profileComparisons=new RouteComparison[simulation.profiles.Length];
            for(int i=0;i<profileComparisons.Length;i++)
            {
                if(i==simulation.profileIndex){profileComparisons[i]=simulation.comparison;continue;}
                var p=simulation.profiles[i];
                profileComparisons[i]=RouteComparison.Create(
                    RouteCalculator.Calculate(simulation.graph,simulation.origin,simulation.destination,p,false),
                    RouteCalculator.Calculate(simulation.graph,simulation.origin,simulation.destination,p,true),p);
            }
            return profileComparisons;
        }
        private void DrawProfiles()
        {
            var comparisons=GetProfileComparisons();
            for(int i=0;i<simulation.profiles.Length;i++)
            {
                var p=simulation.profiles[i];var c=comparisons[i];Gap();GUILayout.Label(p.displayName,body);
                GUILayout.Label($"Largura individual {p.requiredWidth:F2} m · referência {p.referenceFreeWidth:F2} m\nDegrau {p.maxStepHeight:F2} m · subida/descida {p.maxUphillPercent:F1}/{p.maxDownhillPercent:F1}%\nTransversal {p.maxCrossSlopePercent:F1}% · irregularidade {p.maxRoughness:F2}",small);
                GUILayout.Label(c.accessible.route.Found?$"Rota acessível no modelo: {c.accessible.route.distance:F1} m":FailureLabel(c.accessible.route.failure),body);
                GUILayout.Label(p.assumptions,small);
                GUI.enabled=i!=simulation.profileIndex;if(GUILayout.Button("Usar este perfil",button))simulation.SetProfile(i);GUI.enabled=true;
            }
        }
        private void DrawComparisonMetrics(RouteComparison comparison)
        {
            var c=comparison.conventional;var a=comparison.accessible;
            Row("MÉTRICA","Convencional","Acessível");
            Row("Distância (m)",Value(c,c.route?.distance??0,"F1"),Value(a,a.route?.distance??0,"F1"));
            Row("Subida (m)",Value(c,c.route?.ascent??0,"F2"),Value(a,a.route?.ascent??0,"F2"));
            Row("Descida (m)",Value(c,c.descent,"F2"),Value(a,a.descent,"F2"));
            Row("Subida máx. (%)",Value(c,c.maxUphill,"F1"),Value(a,a.maxUphill,"F1"));
            Row("Descida máx. (%)",Value(c,c.maxDownhill,"F1"),Value(a,a.maxDownhill,"F1"));
            Row("Transversal máx. (%)",Value(c,c.maxCrossSlope,"F1"),Value(a,a.maxCrossSlope,"F1"));
            Row("Faixa livre mín. (m)",c.route!=null && c.route.arcs.Count==0?"—":Value(c,c.minClearance,"F2"),a.route!=null && a.route.arcs.Count==0?"—":Value(a,a.minClearance,"F2"));
            Row("Obstáculos",Value(c,c.route?.obstacleCount??0,"F0"),Value(a,a.route?.obstacleCount??0,"F0"));
            Row("Custo ponderado","—",Value(a,a.route?.cost??0,"F1"));
        }
        private static string Value(RouteAnalysis analysis,float value,string format)=>analysis.route!=null && analysis.route.Found?value.ToString(format):"—";
        private void Row(string label,string conventional,string accessible)
        {GUILayout.BeginHorizontal();GUILayout.Label(label,small,GUILayout.Width(80));GUILayout.Label(conventional,small,GUILayout.Width(80));GUILayout.Label(accessible,small,GUILayout.Width(80));GUILayout.EndHorizontal();}
        private static string FailureLabel(RouteFailure failure)
        {
            switch(failure)
            {
                case RouteFailure.InsufficientData:return "Dados insuficientes para confirmar a rota";
                case RouteFailure.Blocked:return "Sem rota: impedimentos no modelo";
                case RouteFailure.NoConnection:return "Origem e destino sem conexão";
                default:return "Rota disponível";
            }
        }
    }
}
