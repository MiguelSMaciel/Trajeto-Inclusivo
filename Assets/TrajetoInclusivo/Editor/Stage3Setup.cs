using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrajetoInclusivo.Editor
{
    public static class Stage3Setup
    {
        [MenuItem("Trajeto Inclusivo/Preparar recursos de obstáculos e relevo")]
        public static void EnsureAssets()
        {
            const string root="Assets/TrajetoInclusivo";
            Directory.CreateDirectory(root+"/Resources");Directory.CreateDirectory(root+"/Data/Elevation");
            string csv="x,z,elevation\n-500,-500,0.18\n0,-500,1.18\n500,-500,2.18\n-500,0,1.18\n0,0,2.18\n500,0,3.18\n-500,500,2.18\n0,500,3.18\n500,500,4.18\n";
            string path=root+"/Resources/ExampleElevation.asset";
            if(!File.Exists(path))
            {
                var dataset=ElevationDataImporter.ParseCsv(csv,"Grade artificial de demonstração — NÃO é relevo de Vitória","Plano local artificial",0,true);
                dataset.sourceDate="2026-10-15";AssetDatabase.CreateAsset(dataset,path);
                File.WriteAllText(root+"/Data/Elevation/example_local_simulated.csv",csv);
            }
            string rampPath=root+"/Prefabs/RampModule.prefab";
            if(!File.Exists(rampPath))
            {
                var go=new GameObject("Rampa SIMULADA");var a=new GameObject("Início").AddComponent<PedestrianNode>();var b=new GameObject("Fim").AddComponent<PedestrianNode>();
                a.transform.SetParent(go.transform);b.transform.SetParent(go.transform);a.transform.localPosition=new Vector3(0,.18f,0);b.transform.localPosition=new Vector3(4,.5f,0);
                a.id="ramp-start";b.id="ramp-end";var segment=go.AddComponent<AccessibilitySegment>();segment.id="ramp";segment.start=a;segment.end=b;
                var ramp=go.AddComponent<AccessibilityRamp>();ramp.segment=segment;ramp.ReadFromSegment();
                PrefabUtility.SaveAsPrefabAsset(go,rampPath);Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("STAGE3_ASSETS_OK");
        }
    }
}
