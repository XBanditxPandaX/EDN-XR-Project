// ShelfSlot.cs
// Represente un emplacement sur une etagere.
// Un ShelfSlot est lie a un seul type d'ingredient (via ingredientData).
// Il instancie le prefab de l'ingredient a sa creation et le respawne
// automatiquement apres un delai configurable lorsqu'il est consomme.
//
// Comment configurer :
//   1. Placez un GameObject vide a l'endroit de l'etagere voulu.
//   2. Ajoutez ce composant.
//   3. Assignez ingredientData (le ScriptableObject IngredientData).
//   4. Assignez ingredientPrefab (prefab avec Ingredient + XRGrabInteractable + Rigidbody).

using System.Collections;
using UnityEngine;

namespace PotionClassroom
{
    public class ShelfSlot : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Inspector
        // ------------------------------------------------------------------
        [Header("Ingredient")]
        [Tooltip("Donnees de l'ingredient geree par cette etagere.")]
        public IngredientData ingredientData;

        [Tooltip("Prefab de l'ingredient a instancier sur l'etagere.")]
        public GameObject ingredientPrefab;

        [Header("Spawn")]
        [Tooltip("Spawn l'ingredient automatiquement au demarrage.")]
        public bool spawnOnStart = true;

        [Tooltip("Override du delai de respawn. -1 = utiliser la valeur de IngredientData.")]
        public float respawnDelayOverride = -1f;

        // ------------------------------------------------------------------
        //  Etat interne
        // ------------------------------------------------------------------
        private GameObject _currentIngredient;
        private Coroutine  _respawnCoroutine;

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------
        private void Start()
        {
            if (spawnOnStart)
                SpawnIngredient();
        }

        // ------------------------------------------------------------------
        //  Spawn / Respawn
        // ------------------------------------------------------------------
        private void SpawnIngredient()
        {
            if (ingredientPrefab == null)
            {
                Debug.LogWarning($"[ShelfSlot] '{name}' : ingredientPrefab non assigne.", this);
                return;
            }

            _currentIngredient = Instantiate(ingredientPrefab, transform.position, transform.rotation);

            // Lie le ShelfSlot a l'ingredient
            Ingredient comp = _currentIngredient.GetComponent<Ingredient>();
            if (comp != null)
            {
                comp.originShelf = this;

                // Si IngredientData n'est pas deja assigne sur le prefab, on l'injecte
                if (comp.data == null && ingredientData != null)
                    comp.data = ingredientData;
            }
            else
            {
                Debug.LogWarning($"[ShelfSlot] Le prefab '{ingredientPrefab.name}' n'a pas de composant Ingredient.", this);
            }
        }

        /// <summary>
        /// Appele par Ingredient.Consume() quand l'ingredient est mis dans le chaudron.
        /// </summary>
        public void OnIngredientConsumed()
        {
            _currentIngredient = null;

            float delay = respawnDelayOverride >= 0f
                ? respawnDelayOverride
                : (ingredientData != null ? ingredientData.respawnDelay : 0f);

            if (delay > 0f)
            {
                if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = StartCoroutine(RespawnAfterDelay(delay));
            }
        }

        private IEnumerator RespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnIngredient();
            _respawnCoroutine = null;
        }

        // ------------------------------------------------------------------
        //  Gizmos (debug visuel dans l'editeur)
        // ------------------------------------------------------------------
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.07f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.15f);

#if UNITY_EDITOR
            if (ingredientData != null)
            {
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.2f,
                    ingredientData.displayName);
            }
#endif
        }
    }
}
