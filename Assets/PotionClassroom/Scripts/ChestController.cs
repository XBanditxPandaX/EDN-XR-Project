using UnityEngine;
using UnityEngine.Events;

namespace PotionClassroom
{
    public class ChestController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("GameObject vide place sur la charniere du couvercle (parent de Chest Top).")]
        public Transform lidHinge;

        [Tooltip("Transform du joueur (rempli automatiquement avec la Camera principale).")]
        public Transform player;

        [Header("Angles (euler locaux du LidHinge)")]
        [Tooltip("Rotation locale du LidHinge quand le coffre est ferme.")]
        public Vector3 closedEuler = Vector3.zero;

        [Tooltip("Rotation locale du LidHinge quand le coffre est ouvert.")]
        public Vector3 openEuler = new Vector3(-100f, 0f, 0f);

        [Header("Comportement")]
        [Tooltip("Distance en metres a laquelle le coffre s'ouvre.")]
        public float openDistance = 18f;

        [Tooltip("Vitesse d'animation ouverture/fermeture.")]
        public float animSpeed = 2f;

        [Header("Evenements")]
        public UnityEvent<PotionResult> onPotionReceived;

        // ------------------------------------------------------------------
        private float _t = 0f;

        // ------------------------------------------------------------------
        private void Awake()
        {
            if (player == null)
                player = Camera.main?.transform;
        }

        private void Update()
        {
            if (lidHinge == null || player == null) return;

            bool shouldOpen = Vector3.Distance(transform.position, player.position) <= openDistance;

            _t = Mathf.Clamp01(_t + Time.deltaTime * animSpeed * (shouldOpen ? 1f : -1f));

            lidHinge.localRotation = Quaternion.Lerp(
                Quaternion.Euler(closedEuler),
                Quaternion.Euler(openEuler),
                _t);
        }

        private void OnTriggerEnter(Collider other)
        {
            PotionResult potion = other.GetComponentInParent<PotionResult>()
                               ?? other.GetComponent<PotionResult>();
            if (potion == null) return;

            Debug.Log($"[Chest] Potion recue : {potion.recipe?.resultPotionName ?? "Inconnue"}");
            onPotionReceived?.Invoke(potion);
            Destroy(potion.gameObject);
        }
    }
}
