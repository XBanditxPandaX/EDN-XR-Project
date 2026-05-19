// StirDetector.cs
// Detecte le geste de touillement dans le chaudron.
//
// Principe : un objet "cuillere" (XRGrabInteractable) doit entrer dans le trigger du chaudron
// et effectuer un mouvement circulaire (au moins N tours) pour declencher OnStirComplete.
//
// Deux modes de detection :
//  - MODE CUILLERE : un prefab de cuillere grabbable tourne physiquement dans le chaudron.
//  - MODE CONTROLLER : le controller droit/gauche fait un geste circulaire dans la zone.
//    (Plus tolerant, recommande pour le prototypage.)
//
// Configuration :
//  - Placez ce script sur le meme GameObject que Cauldron (ou un enfant avec Collider trigger).
//  - Choisissez le mode via stirMode.
//  - Pour MODE CUILLERE : assignez stirrerTag = "Stirrer" et ajoutez ce tag a la cuillere.
//  - Pour MODE CONTROLLER : rien a assigner, le controller XR est detete automatiquement.

using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace PotionClassroom
{
    public class StirDetector : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Enum
        // ------------------------------------------------------------------
        public enum StirMode { Spoon, Controller }

        // ------------------------------------------------------------------
        //  Inspector
        // ------------------------------------------------------------------
        [Header("Mode")]
        public StirMode stirMode = StirMode.Controller;

        [Header("Parametres")]
        [Tooltip("Nombre de rotations completes necessaires pour valider le brassage.")]
        [Min(1)]
        public int requiredTurns = 2;

        [Tooltip("Rayon minimum du mouvement circulaire (en metres) pour etre compte.")]
        [Min(0.01f)]
        public float minCircleRadius = 0.05f;

        [Tooltip("Delai avant remise a zero si aucun mouvement (secondes).")]
        public float idleResetDelay = 3f;

        [Header("Mode Cuillere")]
        [Tooltip("Tag du GameObject cuillere (doit avoir Rigidbody + XRGrabInteractable).")]
        public string stirrerTag = "Stirrer";

        [Header("Feedback")]
        [Tooltip("Son joue pendant le brassage (loop).")]
        public AudioClip stirLoopSound;

        [Tooltip("AudioSource pour jouer le son.")]
        public AudioSource audioSource;

        // ------------------------------------------------------------------
        //  Evenements
        // ------------------------------------------------------------------
        public event Action OnStirStart;
        public event Action OnStirComplete;

        // ------------------------------------------------------------------
        //  Etat interne
        // ------------------------------------------------------------------
        private bool   _trackedObjectInZone = false;
        private Vector3 _lastPosition        = Vector3.zero;
        private Vector3 _center              = Vector3.zero;    // centre de la zone
        private float  _accumulatedAngle     = 0f;              // angle total accumule
        private float  _idleTimer            = 0f;
        private bool   _stirCompleted        = false;

        // Pour le mode Controller : suivi du controller ayant entre dans la zone
        private Transform _trackedTransform  = null;

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------
        private void Awake()
        {
            _center = transform.position;
        }

        private void Update()
        {
            if (!_trackedObjectInZone || _stirCompleted) return;

            if (_trackedTransform == null)
            {
                ExitZone();
                return;
            }

            Vector3 currentPos = _trackedTransform.position;
            Vector3 toCenter   = _center - currentPos;

            // Ignore si trop pres du centre (pas un vrai cercle)
            float radius = new Vector2(toCenter.x, toCenter.z).magnitude;
            if (radius < minCircleRadius)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= idleResetDelay) ResetStir();
                return;
            }

            // Angle dans le plan XZ depuis le dernier frame
            Vector3 lastToCenter    = _center - _lastPosition;
            Vector2 last2D          = new Vector2(lastToCenter.x, lastToCenter.z).normalized;
            Vector2 current2D       = new Vector2(toCenter.x, toCenter.z).normalized;

            float angleDelta = Vector2.SignedAngle(last2D, current2D);
            _accumulatedAngle += angleDelta;
            _idleTimer = 0f;

            _lastPosition = currentPos;

            // Verifie si on a atteint le nombre de tours requis
            float totalDegrees = Mathf.Abs(_accumulatedAngle);
            if (totalDegrees >= 360f * requiredTurns)
            {
                _stirCompleted = true;
                StopStirSound();
                Debug.Log($"[StirDetector] Brassage valide ({requiredTurns} tour(s))!");
                OnStirComplete?.Invoke();
                // Reinitialise pour permettre un nouveau brassage
                Invoke(nameof(ResetStir), 0.5f);
            }
        }

        // ------------------------------------------------------------------
        //  Detection de l'objet dans la zone
        // ------------------------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (_trackedObjectInZone) return;

            if (stirMode == StirMode.Spoon)
            {
                if (!other.CompareTag(stirrerTag)) return;
                EnterZone(other.transform);
            }
            else // Controller
            {
                // Cherche un XRBaseController dans les parents
                XRBaseController ctrl = other.GetComponentInParent<XRBaseController>();
                if (ctrl == null) return;
                EnterZone(other.transform);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_trackedObjectInZone) return;

            bool relevant = stirMode == StirMode.Spoon
                ? other.CompareTag(stirrerTag)
                : other.GetComponentInParent<XRBaseController>() != null;

            if (relevant)
                ExitZone();
        }

        private void EnterZone(Transform trackedTransform)
        {
            _trackedObjectInZone = true;
            _trackedTransform    = trackedTransform;
            _lastPosition        = trackedTransform.position;
            _accumulatedAngle    = 0f;
            _idleTimer           = 0f;
            _stirCompleted       = false;

            PlayStirSound();
            OnStirStart?.Invoke();
            Debug.Log("[StirDetector] Objet dans la zone — suivi commence.");
        }

        private void ExitZone()
        {
            _trackedObjectInZone = false;
            _trackedTransform    = null;
            StopStirSound();
            Debug.Log("[StirDetector] Objet sorti de la zone — suivi arrete.");
        }

        private void ResetStir()
        {
            _accumulatedAngle = 0f;
            _idleTimer        = 0f;
            _stirCompleted    = false;
        }

        // ------------------------------------------------------------------
        //  Audio
        // ------------------------------------------------------------------
        private void PlayStirSound()
        {
            if (audioSource == null || stirLoopSound == null) return;
            if (!audioSource.isPlaying)
            {
                audioSource.clip   = stirLoopSound;
                audioSource.loop   = true;
                audioSource.Play();
            }
        }

        private void StopStirSound()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();
        }

        // ------------------------------------------------------------------
        //  Gizmos
        // ------------------------------------------------------------------
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minCircleRadius);

            float turns  = _accumulatedAngle / 360f;
            float needed = requiredTurns;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.3f,
                $"Tours : {turns:F2} / {needed}");
#endif
        }
    }
}
