using UnityEngine;

namespace PotionClassroom
{
    // Place ce script sur l'XR Origin (XR Rig).
    // Il memorise le Y au spawn et le restaure apres chaque teleportation,
    // evitant le decalage de hauteur du a la recalibration du casque.
    public class TeleportHeightFix : MonoBehaviour
    {
        [Tooltip("Decalage vertical ajoute a la hauteur de spawn (positif = plus haut).")]
        public float heightOffset = 0f;

        private float _lockedY;

        private void Start()
        {
            _lockedY = transform.position.y + heightOffset;
            Apply();
        }

        private void LateUpdate()
        {
            if (Mathf.Abs(transform.position.y - _lockedY) > 0.001f)
                Apply();
        }

        private void Apply()
        {
            Vector3 pos = transform.position;
            pos.y = _lockedY;
            transform.position = pos;
        }
    }
}
