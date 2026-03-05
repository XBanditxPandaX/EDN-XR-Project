// Ingredient.cs
// A placer sur chaque GameObject d'ingredient dans la scene (ou sur son prefab).
// Requiert un XRGrabInteractable et un Rigidbody.
//
// Configuration dans l'Inspector :
//   - ingredientData : assigner le ScriptableObject IngredientData correspondant
//   - originShelf    : reference vers le ShelfSlot parent (optionnel, rempli auto par ShelfSlot)

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PotionClassroom
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class Ingredient : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Inspector
        // ------------------------------------------------------------------
        [Header("Donnees")]
        [Tooltip("ScriptableObject qui decrit cet ingredient (type, nom, couleur...).")]
        public IngredientData data;

        [Header("References")]
        [Tooltip("L'etagere d'origine de cet ingredient (rempli automatiquement par ShelfSlot).")]
        public ShelfSlot originShelf;

        // ------------------------------------------------------------------
        //  Etat interne
        // ------------------------------------------------------------------
        private XRGrabInteractable _grabInteractable;
        private Rigidbody          _rb;
        private bool               _inCauldron = false;

        // ------------------------------------------------------------------
        //  Proprietes publiques
        // ------------------------------------------------------------------
        public IngredientType Type => data != null ? data.ingredientType : IngredientType.None;
        public bool IsInCauldron   => _inCauldron;

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------
        private void Awake()
        {
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _rb               = GetComponent<Rigidbody>();

            // Quand l'ingredient est lache, il peut tomber dans le chaudron
            _grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDestroy()
        {
            if (_grabInteractable != null)
                _grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        // ------------------------------------------------------------------
        //  Evenements XRI
        // ------------------------------------------------------------------
        private void OnReleased(SelectExitEventArgs args)
        {
            // Rien a faire ici : la physique prend le relais.
            // Le Cauldron detecte la collision via OnTriggerEnter.
        }

        // ------------------------------------------------------------------
        //  API appelee par Cauldron
        // ------------------------------------------------------------------
        /// <summary>
        /// Appele par Cauldron quand l'ingredient entre dans la zone du chaudron.
        /// Retourne true si l'ingredient a bien ete "consomme".
        /// </summary>
        public bool Consume()
        {
            if (_inCauldron) return false;   // deja consomme
            if (data == null) return false;

            _inCauldron = true;

            // Prevenir l'etagere qu'elle peut respawner
            if (originShelf != null)
                originShelf.OnIngredientConsumed();

            if (data.destroyOnAdd)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);

            return true;
        }
    }
}
