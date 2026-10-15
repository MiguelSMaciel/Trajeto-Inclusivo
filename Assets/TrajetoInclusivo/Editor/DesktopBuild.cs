using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrajetoInclusivo.Editor
{
    public static class DesktopBuild
    {
        public static void Build(BuildTarget target)
        {
            bool linux=target==BuildTarget.StandaloneLinux64;
            if(!linux && target!=BuildTarget.StandaloneWindows64)throw new ArgumentException("Plataforma desktop não suportada.");
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,target))throw new InvalidOperationException("Instale no Unity Hub o módulo de build para "+target+".");
            Stage3Setup.EnsureAssets();
            if(!File.Exists(TestbedBuilder.ScenePath))TestbedBuilder.CreateProject();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach(var id in AssetDatabase.FindAssets("t:Mesh",new[]{TestbedBuilder.Root+"/Meshes"}))
                {
                    string path=AssetDatabase.GUIDToAssetPath(id);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool changed=false;
                    if(!mesh.HasPreBakeCollisionMesh(false)){mesh.SetPreBakeCollisionMesh(false,true);changed=true;}
                    string expectedName=Path.GetFileNameWithoutExtension(path);
                    if(mesh.name!=expectedName){mesh.name=expectedName;changed=true;}
                    if(changed)EditorUtility.SetDirty(mesh);
                }
            }
            finally{AssetDatabase.StopAssetEditing();}
            PlayerSettings.productName="Trajeto Inclusivo — Avenida Vitória";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            if(linux)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(target,false);
                PlayerSettings.SetGraphicsAPIs(target,new[]{GraphicsDeviceType.OpenGLCore,GraphicsDeviceType.Vulkan});
            }
            AssetDatabase.SaveAssets();
            string folder=linux?"Builds/Linux":"Builds/Windows";
            string executable=linux?"TrajetoInclusivo.x86_64":"TrajetoInclusivo.exe";Directory.CreateDirectory(folder);
            var scenes=File.Exists(GeographicSceneBuilder.FaesaScenePath)?new[]{GeographicSceneBuilder.FaesaScenePath,GeographicSceneBuilder.ScenePath,TestbedBuilder.ScenePath}:
                File.Exists(GeographicSceneBuilder.ScenePath)?new[]{GeographicSceneBuilder.ScenePath,TestbedBuilder.ScenePath}:new[]{TestbedBuilder.ScenePath};
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=folder+"/"+executable,target=target,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build "+target+" falhou: "+result.summary.result);
            CopyDistributionData(folder);
            if(linux)
            {
                File.Copy("Tools/Linux/TrajetoInclusivo.sh",folder+"/TrajetoInclusivo.sh",true);
                File.Copy("Tools/Linux/Testar-Linux.sh",folder+"/Testar-Linux.sh",true);
                File.WriteAllText(folder+"/LEIA-ME-LINUX.txt","Trajeto Inclusivo — Linux x86_64\n\nInicie com ./TrajetoInclusivo.sh\nSe necessário: chmod +x TrajetoInclusivo.sh Testar-Linux.sh TrajetoInclusivo.x86_64\nMantenha toda a pasta. Consulte Documentacao/LINUX.md e Documentacao/README.md.\n");
            }
            Debug.Log("TRAJETO_"+(linux?"LINUX_":"")+"BUILD_OK: "+folder+"/"+executable);
        }
        private static void CopyDistributionData(string folder)
        {
            string data=folder+"/MapData";Directory.CreateDirectory(data);
            if(File.Exists(GeographicSceneBuilder.ScenePath))
            {
                foreach(var file in new[]{"jucutuquara.osm","avenida_vitoria.geojson.json","provenance.json","LICENSE-ODbL-1.0.txt","NOTICE.txt"})
                    File.Copy(TestbedBuilder.Root+"/Data/Geography/"+file,data+"/"+file,true);
                foreach(var script in new[]{"Export-OsmGeoJson.ps1","Export-FaesaGeoJson.ps1"})File.Copy("Tools/"+script,data+"/"+script,true);
                foreach(var source in new[]{"GeographicNetworkBuilder.cs","GeographicMeshBuilder.cs","GeoDataImporter.cs"})File.Copy(TestbedBuilder.Root+"/Scripts/Geo/"+source,data+"/"+source,true);
                GeographicGraphExport.ExportScene(GeographicSceneBuilder.ScenePath,data);
                File.Copy(TestbedBuilder.Root+"/Data/Elevation/example_local_simulated.csv",data+"/example_local_simulated.csv",true);
            }
            if(File.Exists(GeographicSceneBuilder.FaesaScenePath))
            {
                string expanded=data+"/FAESA";Directory.CreateDirectory(expanded);
                foreach(var file in new[]{"avenida_vitoria_faesa.osm","avenida_vitoria.geojson.json","provenance.json","NOTICE.txt","LICENSE-ODbL-1.0.txt"})
                    File.Copy(TestbedBuilder.Root+"/Data/Geography/FAESA/"+file,expanded+"/"+file,true);
                GeographicGraphExport.ExportScene(GeographicSceneBuilder.FaesaScenePath,expanded);
            }
            string documentation=folder+"/Documentacao";Directory.CreateDirectory(documentation);
            string readme=File.ReadAllText("README.md");
            File.WriteAllText(folder+"/README.md",readme.Replace("](Docs/","](Documentacao/"));
            File.WriteAllText(documentation+"/README.md",readme.Replace("](Docs/","]("));
            foreach(var file in Directory.GetFiles("Docs","*",SearchOption.AllDirectories))
            {
                string destination=Path.Combine(documentation,Path.GetRelativePath("Docs",file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file,destination,true);
            }
            File.Copy("NOTICE.md",folder+"/NOTICE.md",true);
        }
    }
}
