using UnityEditor;

namespace TrajetoInclusivo.Editor
{
    public static class LinuxBuild
    {
        [MenuItem("Trajeto Inclusivo/Gerar executável Linux x86_64")]
        public static void Build()=>DesktopBuild.Build(BuildTarget.StandaloneLinux64);
    }
}
