using UnityEngine;

namespace TrajetoInclusivo
{
    [CreateAssetMenu(menuName="Trajeto Inclusivo/Grade de elevação local")]
    public sealed class ElevationDataset:ScriptableObject
    {
        public string sourceDescription;
        public string sourceDate;
        public string verticalDatum;
        public VerificationState verification=VerificationState.ImportedUnverified;
        public int columns,rows;
        public float originX,originZ,spacingX,spacingZ;
        public double verticalOrigin;
        public float[] heights;
        public bool TrySample(float x,float z,out float elevation)
        {
            elevation=0;
            if(columns<2 || rows<2 || heights==null || heights.Length!=columns*rows || spacingX<=0 || spacingZ<=0)return false;
            float gx=(x-originX)/spacingX,gz=(z-originZ)/spacingZ;
            if(gx<0 || gz<0 || gx>columns-1 || gz>rows-1)return false;
            int ix=Mathf.Min(Mathf.FloorToInt(gx),columns-2),iz=Mathf.Min(Mathf.FloorToInt(gz),rows-2);
            float a=Mathf.Lerp(heights[iz*columns+ix],heights[iz*columns+ix+1],gx-ix);
            float b=Mathf.Lerp(heights[(iz+1)*columns+ix],heights[(iz+1)*columns+ix+1],gx-ix);
            elevation=Mathf.Lerp(a,b,gz-iz);return true;
        }
    }
}
