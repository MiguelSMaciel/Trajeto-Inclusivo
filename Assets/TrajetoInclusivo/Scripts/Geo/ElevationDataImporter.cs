using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace TrajetoInclusivo
{
    public static class ElevationDataImporter
    {
        // Local X/Z in metres, matching the geographic scene's origin. No latitude/longitude guessing.
        public static ElevationDataset ParseCsv(string csv,string source,string datum,double verticalOrigin=0,bool simulated=false)
        {
            if(double.IsNaN(verticalOrigin) || double.IsInfinity(verticalOrigin))throw new FormatException("Origem vertical inválida.");
            if(string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(datum))throw new FormatException("Informe a origem dos dados e o datum vertical.");
            var lines=csv.TrimStart('\uFEFF').Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            if(lines.Length<5 || lines[0].Trim()!="x,z,elevation")throw new FormatException("Cabeçalho esperado: x,z,elevation. Use ponto decimal e vírgula separadora.");
            if(lines.Length>65537)throw new FormatException("Grade excede 65.536 amostras; recorte ou reamostre o arquivo.");
            var samples=new Dictionary<(double x,double z),double>();var xs=new SortedSet<double>();var zs=new SortedSet<double>();
            foreach(var line in lines.Skip(1))
            {
                var fields=line.Split(',');if(fields.Length!=3)throw new FormatException("Cada linha deve conter x,z,elevation.");
                double x=Number(fields[0]),z=Number(fields[1]),y=Number(fields[2]);
                if(Math.Abs(x)>100000 || Math.Abs(z)>100000 || Math.Abs(y-verticalOrigin)>10000)throw new FormatException("Valores fora da faixa local admitida.");
                if(samples.ContainsKey((x,z)))throw new FormatException("Coordenada duplicada na grade.");
                samples.Add((x,z),y);xs.Add(x);zs.Add(z);
            }
            if(xs.Count<2 || zs.Count<2 || samples.Count!=(long)xs.Count*zs.Count)throw new FormatException("A grade deve ser retangular e completa, com ao menos 2 × 2 pontos.");
            var xx=xs.ToArray();var zz=zs.ToArray();double dx=xx[1]-xx[0],dz=zz[1]-zz[0];
            if(dx<.01 || dz<.01)throw new FormatException("Espaçamento da grade deve ser ao menos 1 cm.");
            for(int i=2;i<xx.Length;i++)if(Math.Abs((xx[i]-xx[i-1])-dx)>Math.Max(.00001,dx*1e-6))throw new FormatException("Espaçamento X irregular.");
            for(int i=2;i<zz.Length;i++)if(Math.Abs((zz[i]-zz[i-1])-dz)>Math.Max(.00001,dz*1e-6))throw new FormatException("Espaçamento Z irregular.");
            var result=ScriptableObject.CreateInstance<ElevationDataset>();result.columns=xx.Length;result.rows=zz.Length;
            result.originX=(float)xx[0];result.originZ=(float)zz[0];result.spacingX=(float)dx;result.spacingZ=(float)dz;
            result.sourceDescription=source;result.verticalDatum=datum;result.verticalOrigin=verticalOrigin;
            result.verification=simulated?VerificationState.Simulated:VerificationState.ImportedUnverified;
            result.heights=new float[samples.Count];
            for(int j=0;j<zz.Length;j++)for(int i=0;i<xx.Length;i++)result.heights[j*xx.Length+i]=(float)(samples[(xx[i],zz[j])]-verticalOrigin);
            return result;
        }
        public static void Validate(ElevationDataset data)
        {
            if(string.IsNullOrWhiteSpace(data.sourceDescription) || string.IsNullOrWhiteSpace(data.verticalDatum) || double.IsNaN(data.verticalOrigin) || double.IsInfinity(data.verticalOrigin) ||
                data.columns<2 || data.rows<2 || (long)data.columns*data.rows>65536 || data.heights==null || data.heights.Length!=(long)data.columns*data.rows || data.spacingX<.01f || data.spacingZ<.01f ||
                !Finite(data.spacingX) || !Finite(data.spacingZ) || !Finite(data.originX) || !Finite(data.originZ) || data.heights.Any(y=>!Finite(y) || Mathf.Abs(y)>10000) ||
                (data.verification!=VerificationState.Simulated && data.verification!=VerificationState.ImportedUnverified))throw new FormatException("Grade de elevação inválida.");
        }
        private static bool Finite(float value)=>!float.IsNaN(value) && !float.IsInfinity(value);
        private static double Number(string value)
        {
            if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var number) || double.IsInfinity(number) || double.IsNaN(number))throw new FormatException("Número inválido na grade de elevação.");
            return number;
        }
    }
}
