using UnityEngine;

namespace TrajetoInclusivo
{
    [CreateAssetMenu(menuName = "Trajeto Inclusivo/Perfil de mobilidade")]
    public sealed class AccessibilityProfile : ScriptableObject
    {
        public string displayName = "Cadeira de rodas manual";
        [TextArea] public string assumptions = "Hipóteses de teste; não são limites normativos ou clínicos.";
        [Min(.1f)] public float requiredWidth = .85f;
        [Min(0)] public float maxStepHeight = .02f;
        [Min(0)] public float maxUphillPercent = 8f;
        [Min(0)] public float maxDownhillPercent = 10f;
        [Min(0)] public float maxCrossSlopePercent = 3f;
        [Range(0, 1)] public float maxRoughness = .6f;
        [Min(.1f)] public float referenceFreeWidth = 1.2f;
        public CostWeights weights = new CostWeights();
        public static event System.Action ProfileChanged;
        private void OnValidate() => ProfileChanged?.Invoke();
    }

    [System.Serializable]
    public sealed class CostWeights
    {
        [Min(0)] public float uphillPerPercent = .10f;
        [Min(0)] public float downhillPerPercent = .06f;
        [Min(0)] public float crossSlopePerPercent = .08f;
        [Min(0)] public float roughness = 1.5f;
        [Min(0)] public float narrowness = 2f;
        [Min(0)] public float obstacle = .2f;
    }

    public enum AccessibilityRating { Accessible, Difficult, Inaccessible, NotEvaluated }
    // Provenance and completeness are independent: synthetic data may be fully specified.
    public enum VerificationState { Simulated, ImportedUnverified, FieldVerified }
    public enum ObstacleKind { Step, Pothole, Construction, Pole, Tree, Bin, Furniture, Generic }
}
