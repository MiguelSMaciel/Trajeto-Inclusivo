using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class PedestrianNode : MonoBehaviour
    {
        public string id;
        public string displayName;
        public VerificationState elevationVerification=VerificationState.Simulated;
        public string elevationSource="Cota artificial do cenário";
        public Vector3 Position => transform.position;
    }
}
