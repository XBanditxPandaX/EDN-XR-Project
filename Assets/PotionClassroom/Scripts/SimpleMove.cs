using UnityEngine;
using UnityEngine.XR;

namespace PotionClassroom
{
    public class SimpleMove : MonoBehaviour
    {
        [Tooltip("Transform de l'XR Origin (XR Rig) a deplacer.")]
        public Transform xrOrigin;

        [Tooltip("Vitesse de deplacement en m/s.")]
        public float speed = 3f;

        private void Update()
        {
            InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!left.isValid) return;

            if (!left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis)) return;
            if (axis.magnitude < 0.1f) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 right   = Vector3.ProjectOnPlane(cam.transform.right,   Vector3.up).normalized;
            Vector3 move    = (forward * axis.y + right * axis.x) * speed * Time.deltaTime;

            xrOrigin.position += move;
        }
    }
}
