using System.IO;
using UnityEditor;

namespace TrajetoInclusivo.Editor
{
    [InitializeOnLoad]
    public static class FirstOpenSetup
    {
        static FirstOpenSetup()
        {
            // Batch setup is explicit; interactive first opening prepares the runnable scene automatically.
            if (UnityEngine.Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(TestbedBuilder.ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    TestbedBuilder.CreateProject();
            };
        }
    }
}
