using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PotionClassroom
{
    public class ChestController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("GameObject vide place sur la charniere du couvercle (parent de Chest Top).")]
        public Transform lidHinge;

        [Tooltip("Transform du joueur (rempli automatiquement avec la Camera principale).")]
        public Transform player;

        [Tooltip("OrderManager pour notifier les depots de potions.")]
        public OrderManager orderManager;

        [Header("Angles (euler locaux du LidHinge)")]
        public Vector3 closedEuler = Vector3.zero;
        public Vector3 openEuler   = new Vector3(-100f, 0f, 0f);

        [Header("Comportement")]
        public float openDistance = 18f;
        public float animSpeed    = 2f;

        [Header("Stockage des potions")]
        [Tooltip("Position locale du centre de depot (interieur du coffre).")]
        public Vector3 depositLocalPosition = new Vector3(0f, 0.1f, 0f);

        [Tooltip("Ecart horizontal entre les potions deposees.")]
        public float depositSpacing = 0.06f;

        [Tooltip("Scale appliquee aux potions une fois dans le coffre.")]
        public float depositScale = 0.4f;

        // ------------------------------------------------------------------
        private float _t = 0f;
        private readonly List<GameObject>    _storedObjects = new List<GameObject>();
        private readonly List<PotionRecipe>  _storedRecipes = new List<PotionRecipe>();

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
            CraftedPotion marker = other.GetComponentInParent<CraftedPotion>()
                                ?? other.GetComponent<CraftedPotion>();
            if (marker == null) return;

            StorePotion(marker);
        }

        // ------------------------------------------------------------------
        private void StorePotion(CraftedPotion marker)
        {
            GameObject go = marker.gameObject;

            // Desactive la physique et le grab
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.useGravity = false; rb.velocity = Vector3.zero; }

            XRGrabInteractable grab = go.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.enabled = false;

            // Place la potion au fond du coffre
            go.transform.SetParent(transform);
            float xOffset = (_storedObjects.Count - (_storedObjects.Count > 0 ? 0 : 0)) * depositSpacing;
            xOffset = _storedObjects.Count * depositSpacing;
            go.transform.localPosition = depositLocalPosition + Vector3.right * xOffset;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one * depositScale;

            _storedObjects.Add(go);
            _storedRecipes.Add(marker.recipe);

            Debug.Log($"[Chest] Potion stockee : {marker.recipe?.resultPotionName ?? "Inconnue"}");
            orderManager?.OnPotionDeposited(marker.recipe);
        }

        // ------------------------------------------------------------------
        public List<PotionRecipe> GetStoredRecipes() => new List<PotionRecipe>(_storedRecipes);

        public void ClearChest()
        {
            foreach (GameObject go in _storedObjects)
                if (go != null) Destroy(go);
            _storedObjects.Clear();
            _storedRecipes.Clear();
        }
    }
}
