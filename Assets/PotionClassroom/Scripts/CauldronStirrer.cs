using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PotionClassroom
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public class CauldronStirrer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Le baton dans le chaudron (Cylinder002).")]
        public Transform stirrer;

        [Tooltip("Le script Cauldron sur ce meme objet ou un parent.")]
        public Cauldron cauldron;

        [Header("Animation")]
        [Tooltip("Nombre de tours effectues avant de declencher le brassage.")]
        [Min(1)]
        public int turns = 2;

        [Tooltip("Vitesse de rotation en degres par seconde.")]
        public float rotationSpeed = 180f;

        [Tooltip("Rayon du cercle que decrit le baton.")]
        public float stirRadius = 0.15f;

        public event System.Action OnStirStart;
        public event System.Action OnStirEnd;

        private bool _isStirring = false;
        private Vector3 _stirrerStartLocalPos;
        private XRSimpleInteractable _interactable;

        private void Awake()
        {
            _interactable = GetComponent<XRSimpleInteractable>();
            // selectEntered se declenche au clic du laser (trigger = select)
            _interactable.selectEntered.AddListener(OnSelected);

            if (stirrer != null)
                _stirrerStartLocalPos = stirrer.localPosition;

            if (cauldron == null)
                cauldron = GetComponent<Cauldron>() ?? GetComponentInParent<Cauldron>();
        }

        private void OnDestroy()
        {
            if (_interactable != null)
                _interactable.selectEntered.RemoveListener(OnSelected);
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            // Relache immediatement la selection (on veut juste detecter le clic)
            args.manager.SelectExit(args.interactorObject, args.interactableObject);

            if (_isStirring)
            {
                Debug.Log("[CauldronStirrer] Deja en train de touiller.");
                return;
            }

            if (cauldron == null)
            {
                Debug.LogWarning("[CauldronStirrer] Cauldron non assigne !");
                return;
            }

            if (cauldron.AddedIngredients.Count == 0)
            {
                Debug.Log("[CauldronStirrer] Chaudron vide, pas de touillement.");
                return;
            }

            Debug.Log($"[CauldronStirrer] Debut du touillement ({cauldron.AddedIngredients.Count} ingredient(s)).");
            StartCoroutine(StirRoutine());
        }

        private IEnumerator StirRoutine()
        {
            _isStirring = true;
            OnStirStart?.Invoke();

            float totalAngle = 360f * turns;
            float accumulated = 0f;

            while (accumulated < totalAngle)
            {
                float delta = rotationSpeed * Time.deltaTime;
                accumulated += delta;

                float angleRad = accumulated * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angleRad) * stirRadius,
                    0f,
                    Mathf.Sin(angleRad) * stirRadius);

                if (stirrer != null)
                    stirrer.localPosition = _stirrerStartLocalPos + offset;

                yield return null;
            }

            if (stirrer != null)
                stirrer.localPosition = _stirrerStartLocalPos;

            _isStirring = false;
            OnStirEnd?.Invoke();

            Debug.Log("[CauldronStirrer] Touillement termine, brassage lance.");
            cauldron.HandleStirComplete();
        }
    }
}
