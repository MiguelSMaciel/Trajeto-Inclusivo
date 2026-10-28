using UnityEngine;

namespace TrajetoInclusivo
{
    public static class DesktopRuntimeDefaults
    {
        public const int TargetFrameRate=60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Explicit pacing avoids rendering without a limit on high-refresh displays.
            QualitySettings.vSyncCount=0;
            Application.targetFrameRate=TargetFrameRate;
        }
    }
}
