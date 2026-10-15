using UnityEditor;

namespace TrajetoInclusivo.Editor
{
    public static class WindowsBuild
    {
        [MenuItem("Trajeto Inclusivo/Gerar executável Windows")]
        public static void Build()=>DesktopBuild.Build(BuildTarget.StandaloneWindows64);
    }
}
