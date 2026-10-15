using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class AccessibilityRamp:MonoBehaviour
    {
        public AccessibilitySegment segment;
        public float startElevation,endElevation;
        [Min(.1f)]public float width=2.4f;
        public float crossSlopePercent;
        public float Length=>segment==null?0:segment.HorizontalDistance;
        public float LongitudinalSlopePercent=>Length>.001f?100*(endElevation-startElevation)/Length:0;
        public void ReadFromSegment()
        {
            startElevation=segment.start.Position.y;endElevation=segment.end.Position.y;
            width=segment.totalWidth;crossSlopePercent=segment.crossSlopePercent;
        }
        [ContextMenu("Aplicar rampa SIMULADA ao trecho")]
        public void Apply()
        {
            if(segment==null || segment.start==null || segment.end==null)return;
            var a=segment.start.Position;var b=segment.end.Position;a.y=startElevation;b.y=endElevation;
            segment.start.transform.position=a;segment.end.transform.position=b;
            segment.start.elevationVerification=segment.end.elevationVerification=VerificationState.Simulated;
            segment.start.elevationSource=segment.end.elevationSource="Edição artificial de rampa";
            segment.totalWidth=Mathf.Max(.1f,width);segment.freeWidth=Mathf.Min(segment.freeWidth,segment.totalWidth);
            segment.crossSlopePercent=crossSlopePercent;segment.elevationIsSimulated=true;
            segment.NotifyChanged();
        }
    }
}
