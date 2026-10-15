using UnityEngine;

namespace TrajetoInclusivo
{
    [CreateAssetMenu(menuName = "Trajeto Inclusivo/Cenário artificial")]
    public sealed class ScenarioDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public string raisedNodeId = "M";
        public float elevationOffset = .6f;
        public ObstacleSpecification[] obstacles = new ObstacleSpecification[0];
    }

    [System.Serializable]
    public sealed class ObstacleSpecification
    {
        public string segmentId = "C-M";
        public ObstacleKind kind;
        public float heightOrDepth = .16f;
        public float remainingFreeWidth = 2.4f;
        public float length = 1.2f;
        public float severity = .35f;
        public bool canBypass = true;
        public bool blocksPassage;
    }
}
