using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class RouteInspection : MonoBehaviour
    {
        public SimulationController simulation;
        public CameraController cameraController;
        public Transform marker;
        [Min(.1f)] public float speedMetresPerSecond = 2;
        private static readonly float[] playbackSpeeds = { .5f, 1f, 1.5f, 2f, 4f, 8f };
        public int PlaybackSpeedIndex { get; private set; } = 1;
        public float PlaybackSpeedMultiplier => playbackSpeeds[PlaybackSpeedIndex];
        public bool Running { get; private set; }
        private RouteResult route;
        private int edge;
        private float travelled;

        private void Start() { marker.gameObject.SetActive(false); simulation.RoutesChanged += Stop; }
        public void SetPlaybackSpeedIndex(int index)
        {
            if (index < 0 || index >= playbackSpeeds.Length) throw new System.ArgumentOutOfRangeException(nameof(index));
            PlaybackSpeedIndex = index;
        }
        public void Begin()
        {
            Stop();
            route = simulation.accessible;
            if (route == null || !route.Found || route.arcs.Count == 0) return;
            edge = 0; travelled = 0; Running = true;
            marker.gameObject.SetActive(true); marker.position = route.nodes[0].Position;
            cameraController.followTarget = marker; cameraController.distance = 18;
        }
        private void Update()
        {
            if (!Running) return;
            travelled += speedMetresPerSecond * PlaybackSpeedMultiplier * Time.deltaTime;
            while (edge < route.arcs.Count && travelled >= route.arcs[edge].segment.Length)
            {
                travelled -= route.arcs[edge].segment.Length; edge++;
            }
            if (edge >= route.arcs.Count)
            {
                marker.position = route.nodes[route.nodes.Count - 1].Position;
                Running = false; cameraController.followTarget = null; return;
            }
            var a = route.nodes[edge].Position; var b = route.nodes[edge + 1].Position;
            marker.position = Vector3.Lerp(a, b, travelled / Mathf.Max(.001f, Vector3.Distance(a, b)));
            marker.rotation = Quaternion.LookRotation((b - a).normalized);
        }
        public void Stop()
        {
            Running = false;
            if (marker != null) marker.gameObject.SetActive(false);
            if (cameraController != null) cameraController.followTarget = null;
        }
        private void OnDestroy() { if (simulation != null) simulation.RoutesChanged -= Stop; }
    }
}
