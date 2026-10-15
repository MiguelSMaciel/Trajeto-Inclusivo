using UnityEngine;

namespace TrajetoInclusivo
{
    public sealed class CameraController : MonoBehaviour
    {
        public SimulationUI ui;
        public Camera view;
        public Vector3 target;
        public float distance = 110;
        public float yaw = 0;
        public float pitch = 58;
        public bool topDown;
        public Transform followTarget;
        public Vector3 frameCenter;
        public Vector2 frameSize = new Vector2(138, 90);

        private void Start() => FrameMap();

        private void LateUpdate()
        {
            if (ui != null) view.rect = ui.SceneViewport;
            bool pointerInUI = ui != null && ui.ContainsPointer();
            bool keyboardBlocked=ui!=null && ui.TextInputFocused;
            if (!keyboardBlocked && Input.GetKeyDown(KeyCode.T)) ToggleView();
            if (!keyboardBlocked && Input.GetKeyDown(KeyCode.Home)) FrameMap();
            if (followTarget != null) target = followTarget.position;
            else if (!pointerInUI && !keyboardBlocked)
            {
                var flatRotation = Quaternion.Euler(0, yaw, 0);
                var motion = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
                if (Input.GetKey(KeyCode.Q)) motion.y -= 1;
                if (Input.GetKey(KeyCode.E)) motion.y += 1;
                target += flatRotation * motion * Time.deltaTime * distance * .45f;
            }
            if (!pointerInUI)
            {
                distance = Mathf.Clamp(distance * (1 - Input.mouseScrollDelta.y * .09f), 6, 2500);
                if (Input.GetMouseButton(1) && !topDown)
                {
                    yaw += Input.GetAxis("Mouse X") * 4;
                    pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3, 15, 85);
                }
            }
            var rotation = Quaternion.Euler(topDown ? 90 : pitch, yaw, 0);
            transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
            view.orthographic = topDown;
            if (topDown) view.orthographicSize = distance * .55f;
        }
        public void ToggleView() => topDown = !topDown;
        public void FrameMap()
        {
            followTarget = null; target = frameCenter; yaw = 0; pitch = 58;
            if (ui != null) view.rect = ui.SceneViewport;
            distance = DistanceForArea(frameSize);
        }
        public void FrameRoute(RouteResult route)
        {
            if (route == null || !route.Found) return;
            var bounds = new Bounds(route.nodes[0].Position, Vector3.zero);
            foreach (var node in route.nodes) bounds.Encapsulate(node.Position);
            followTarget = null; target = bounds.center; yaw = 0; pitch = 58;
            distance = DistanceForArea(new Vector2(Mathf.Max(80,bounds.size.x+35), Mathf.Max(80,bounds.size.z+35)));
        }
        public void FrameSegment(AccessibilitySegment segment)
        {
            if(segment==null)return;
            followTarget=null;target=(segment.start.Position+segment.end.Position)*.5f;pitch=58;
            var d=segment.end.Position-segment.start.Position;yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg+55;
            float aspect=Mathf.Max(.2f,view.pixelRect.width/Mathf.Max(1,view.pixelRect.height));
            distance=Mathf.Clamp(Mathf.Max(12,(segment.Length+8)/aspect),12,500);
        }
        public void FramePoint(Vector3 point)
        {
            followTarget=null;target=point;pitch=58;yaw=0;
            if(ui!=null)view.rect=ui.SceneViewport;
            float aspect=Mathf.Max(.2f,view.pixelRect.width/Mathf.Max(1,view.pixelRect.height));
            distance=Mathf.Max(45,30/(Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f)*aspect));
        }
        private float DistanceForArea(Vector2 size)
        {
            float aspect = Mathf.Max(.2f, view.pixelRect.width / Mathf.Max(1, view.pixelRect.height));
            float tangent = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
            return Mathf.Max(90, size.x / (2 * tangent * aspect), size.y / (2 * tangent));
        }
    }
}
