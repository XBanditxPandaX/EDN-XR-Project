// PotionResult.cs
// Composant attache au prefab de la potion resultante.
// Contient la reference a sa recette et expose des comportements optionnels
// (ramassable, auto-destruction, effets en main).
//
// Mettez ce script sur le prefab "resultPrefab" de chaque PotionRecipe.
// Il est optionnel : le systeme fonctionne sans si vous n'avez pas de prefab.

using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PotionClassroom
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class PotionResult : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Inspector
        // ------------------------------------------------------------------
        [Header("Donnees de la potion")]
        [Tooltip("Recette dont cette potion est le resultat (rempli automatiquement par Cauldron).")]
        public PotionRecipe recipe;

        [Header("Comportement")]
        [Tooltip("Detruit la potion apres ce delai si elle n'est pas ramassee (0 = jamais).")]
        [Min(0f)]
        public float autoDestroyDelay = 30f;

        [Tooltip("Son joue quand la potion est ramassee.")]
        public AudioClip pickupSound;

        [Tooltip("AudioSource pour jouer le son (optionnel).")]
        public AudioSource audioSource;

        [Header("Renderer")]
        [Tooltip("Renderer de la fiole a teinter avec la couleur de la potion.")]
        public Renderer flaskRenderer;

        [Tooltip("Index du materiau a teinter.")]
        public int flaskMaterialIndex = 0;

        [Tooltip("Propriete shader pour la couleur du liquide.")]
        public string flaskColorProperty = "_Color";

        // ------------------------------------------------------------------
        //  References internes
        // ------------------------------------------------------------------
        private XRGrabInteractable _grab;

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------
        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            _grab.selectEntered.AddListener(OnPickedUp);

            if (autoDestroyDelay > 0f)
                StartCoroutine(AutoDestroyRoutine());
        }

        private void OnDestroy()
        {
            if (_grab != null)
                _grab.selectEntered.RemoveListener(OnPickedUp);
        }

        // ------------------------------------------------------------------
        //  Initialisation (appelee par Cauldron apres l'instantiation)
        // ------------------------------------------------------------------
        /// <summary>
        /// Initialise la potion avec la recette correspondante.
        /// Applique la couleur et le nom.
        /// </summary>
        public void Initialize(PotionRecipe potionRecipe)
        {
            recipe = potionRecipe;
            if (recipe == null) return;

            // Teinte la fiole
            ApplyColor(recipe.resultColor);

            Debug.Log($"[PotionResult] Potion cree : {recipe.resultPotionName}");
        }

        // ------------------------------------------------------------------
        //  Evenements
        // ------------------------------------------------------------------
        private void OnPickedUp(SelectEnterEventArgs args)
        {
            if (audioSource != null && pickupSound != null)
                audioSource.PlayOneShot(pickupSound);

            if (recipe != null)
                Debug.Log($"[PotionResult] Potion ramassee : {recipe.resultPotionName}");
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------
        private void ApplyColor(Color color)
        {
            if (flaskRenderer == null) return;
            Material[] mats = flaskRenderer.materials;
            if (flaskMaterialIndex >= mats.Length) return;
            if (mats[flaskMaterialIndex].HasProperty(flaskColorProperty))
                mats[flaskMaterialIndex].SetColor(flaskColorProperty, color);
        }

        private IEnumerator AutoDestroyRoutine()
        {
            yield return new WaitForSeconds(autoDestroyDelay);
            // Ne pas detruire si la potion est tenue en main
            if (_grab == null || !_grab.isSelected)
                Destroy(gameObject);
        }
    }
}
