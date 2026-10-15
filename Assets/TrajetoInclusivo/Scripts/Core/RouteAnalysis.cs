using System.Collections.Generic;
using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class RouteSegmentAnalysis
    {
        public AccessibilitySegment segment;
        public bool forward;
        public AccessibilityRating rating;
        public string reason;
        public float slope,clearance;
    }

    // Computed when routes change; GUI repaint does not run the evaluator again.
    public sealed class RouteAnalysis
    {
        public RouteResult route;
        public float descent,maxUphill,maxDownhill,maxCrossSlope,minClearance;
        public int blockedCount,unknownCount,difficultCount;
        public readonly List<RouteSegmentAnalysis> segments=new List<RouteSegmentAnalysis>();
        public static RouteAnalysis Create(RouteResult route,AccessibilityProfile profile)
        {
            var result=new RouteAnalysis{route=route};
            if(route==null || !route.Found)return result;
            result.minClearance=route.arcs.Count==0?0:float.PositiveInfinity;
            foreach(var arc in route.arcs)
            {
                var s=arc.segment;var e=AccessibilityEvaluator.Evaluate(s,profile,arc.forward);
                float slope=s.SlopePercent*(arc.forward?1:-1);
                float clearance=ObstacleFootprints.FreeWidth(s,profile.maxStepHeight);
                result.segments.Add(new RouteSegmentAnalysis{segment=s,forward=arc.forward,rating=e.rating,reason=e.reason,slope=slope,clearance=clearance});
                result.descent+=Mathf.Max(0,(s.start.Position.y-s.end.Position.y)*(arc.forward?1:-1));
                result.maxUphill=Mathf.Max(result.maxUphill,slope);result.maxDownhill=Mathf.Max(result.maxDownhill,-slope);
                result.maxCrossSlope=Mathf.Max(result.maxCrossSlope,Mathf.Abs(s.crossSlopePercent));result.minClearance=Mathf.Min(result.minClearance,clearance);
                if(e.rating==AccessibilityRating.Inaccessible)result.blockedCount++;
                else if(e.rating==AccessibilityRating.NotEvaluated)result.unknownCount++;
                else if(e.rating==AccessibilityRating.Difficult)result.difficultCount++;
            }
            return result;
        }
    }

    public sealed class RouteComparison
    {
        public RouteAnalysis conventional,accessible;
        public string explanation;
        public bool HasBoth=>conventional.route!=null && accessible.route!=null && conventional.route.Found && accessible.route.Found;
        public float ExtraDistance=>HasBoth?Mathf.Max(0,accessible.route.distance-conventional.route.distance):0;
        public float ExtraPercent=>HasBoth && conventional.route.distance>.001f?100*ExtraDistance/conventional.route.distance:0;
        public static RouteComparison Create(RouteResult conventional,RouteResult accessible,AccessibilityProfile profile)
        {
            var result=new RouteComparison{conventional=RouteAnalysis.Create(conventional,profile),accessible=RouteAnalysis.Create(accessible,profile)};
            if(accessible==null || conventional==null)result.explanation="Calculando os trajetos.";
            else if(!accessible.Found)result.explanation=accessible.explanation;
            else if(!conventional.Found)result.explanation=conventional.explanation;
            else if(result.conventional.blockedCount>0)result.explanation=$"O caminho convencional contém {result.conventional.blockedCount} trecho(s) impedido(s) para este perfil. A busca acessível exclui essas passagens e escolhe o menor custo restante.";
            else if(result.conventional.unknownCount>0)result.explanation=$"O caminho convencional contém {result.conventional.unknownCount} trecho(s) sem avaliação. A busca acessível usa somente trechos avaliados e passáveis.";
            else
            {
                bool same=conventional.arcs.Count==accessible.arcs.Count;
                for(int i=0;same && i<conventional.arcs.Count;i++)same=conventional.arcs[i].segment==accessible.arcs[i].segment && conventional.arcs[i].forward==accessible.arcs[i].forward;
                result.explanation=same?"As duas buscas escolheram o mesmo caminho. Ele tem a menor distância e o menor custo ponderado entre as opções passáveis avaliadas."
                    :"As duas rotas são passáveis para o perfil. A busca acessível escolheu outro caminho pelo menor custo ponderado de inclinação, piso, largura e obstáculos.";
            }
            return result;
        }
    }
}
