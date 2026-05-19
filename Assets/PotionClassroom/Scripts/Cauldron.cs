// Cauldron.cs
// Coeur du systeme de potions.
// A placer sur le GameObject "cauldron scene" (ou un enfant avec un Collider trigger).
//
// Fonctionnement :
//  1. Quand un ingredient tombe dans la zone trigger du chaudron, il est enregistre.
//  2. Le joueur doit touiller (via StirDetector) pour declencher la verification des recettes.
//  3. Si une recette correspond, la potion est creee (son, VFX, prefab).
//  4. Si aucune recette, le chaudron signale un echec et vide son contenu.
//
// Configuration :
//  - Ajoutez un Collider (IsTrigger = true) sur le meme GameObject ou un enfant.
//  - Remplissez la liste "recipes" avec vos ScriptableObjects PotionRecipe.
//  - Assignez cauldronLiquidRenderer si vous voulez teinter le liquide.
//  - Assignez stirDetector (ou le script se charge de le trouver automatiquement).

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace PotionClassroom
{
    public class Cauldron : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Inspector
        // ------------------------------------------------------------------
        [Header("Recettes")]
        [Tooltip("Toutes les recettes de potions connues. L'ordre definit la priorite si plusieurs recettes correspondent.")]
        public PotionRecipe[] recipes;

        [Header("References")]
        [Tooltip("Renderer du liquide du chaudron (pour changer sa couleur). Optionnel.")]
        public Renderer cauldronLiquidRenderer;

        [Tooltip("Index du materiau du liquide dans le renderer (defaut 0).")]
        public int liquidMaterialIndex = 0;

        [Tooltip("Propriete shader pour la couleur du liquide (defaut _Color).")]
        public string liquidColorProperty = "_Color";

        [Tooltip("Point de spawn de la potion resultante au-dessus du chaudron.")]
        public Transform potionSpawnPoint;

        [Tooltip("Referece vers le StirDetector (cherche automatiquement si non assigne).")]
        public StirDetector stirDetector;


        [Header("Feedback sonore")]
        [Tooltip("AudioSource pour les sons du chaudron.")]
        public AudioSource audioSource;

        [Tooltip("Son joue quand un ingredient est ajoute.")]
        public AudioClip ingredientAddedSound;

        [Tooltip("Son joue quand la recette echoue.")]
        public AudioClip recipeFailSound;

        [Tooltip("Son joue quand une potion est creee avec succes.")]
        public AudioClip brewSuccessSound;
        [Range(0f, 1f)] public float brewSuccessSoundVolume = 1f;

        [Tooltip("Son de bouillonnement (loop) pendant la preparation.")]
        public AudioClip boilingLoopSound;

        [Tooltip("Volume du bouillonnement (0-1).")]
        [Range(0f, 1f)] public float boilingVolume = 0.45f;

        private AudioSource _loopSource;

        [Header("Evenements")]
        [Tooltip("Declenche quand un ingredient est ajoute. Passe le nom de l'ingredient.")]
        public UnityEvent<string> onIngredientAdded;

        [Tooltip("Declenche quand une potion est reussie. Passe la recette resultante.")]
        public UnityEvent<PotionRecipe> onPotionBrewed;

        [Tooltip("Declenche quand le brassage echoue (mauvaise combinaison).")]
        public UnityEvent onBrewFailed;

        [Tooltip("Declenche quand le chaudron est vide/reinitialise.")]
        public UnityEvent onCauldronReset;

        // ------------------------------------------------------------------
        //  Etat interne
        // ------------------------------------------------------------------
        private readonly List<IngredientType> _addedIngredients = new List<IngredientType>();
        private bool _brewing = false;          // verrou pendant l'animation de brassage
        private Color _defaultLiquidColor;

        // Couleur courante du liquide (melange des ingredients)
        private Color _currentLiquidColor = Color.clear;

        // ------------------------------------------------------------------
        //  Proprietes publiques (lues par CauldronUI)
        // ------------------------------------------------------------------
        public IReadOnlyList<IngredientType> AddedIngredients => _addedIngredients;
        public bool IsBrewing => _brewing;

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------
        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake  = false;
            }
            audioSource.spatialBlend = 0f;

            // AudioSource loop pour le bouillonnement
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.loop         = true;
            _loopSource.playOnAwake  = false;
            _loopSource.volume       = boilingVolume;
            _loopSource.spatialBlend = 1f;

            if (stirDetector == null)
                stirDetector = GetComponentInChildren<StirDetector>();

            if (stirDetector != null)
                stirDetector.OnStirComplete += HandleStirComplete;

            // Sauvegarde la couleur par defaut du liquide
            if (cauldronLiquidRenderer != null)
            {
                Material mat = cauldronLiquidRenderer.materials[liquidMaterialIndex];
                if (mat.HasProperty(liquidColorProperty))
                    _defaultLiquidColor = mat.GetColor(liquidColorProperty);
                else
                    _defaultLiquidColor = Color.black;
            }
        }

        private void OnDestroy()
        {
            if (stirDetector != null)
                stirDetector.OnStirComplete -= HandleStirComplete;
        }

        // ------------------------------------------------------------------
        //  Trigger : detection des ingredients
        // ------------------------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (_brewing) return;

            Ingredient ingredient = other.GetComponentInParent<Ingredient>();
            if (ingredient == null)
                ingredient = other.GetComponent<Ingredient>();

            if (ingredient == null || ingredient.IsInCauldron) return;
            if (ingredient.data == null)                        return;

            // Consomme l'ingredient
            IngredientType type     = ingredient.Type;
            string         ingName  = ingredient.data.displayName;
            Color          ingColor = ingredient.data.cauldronTintColor;

            bool consumed = ingredient.Consume();
            if (!consumed) return;

            _addedIngredients.Add(type);
            BlendLiquidColor(ingColor);
            PlaySound(ingredientAddedSound);

            if (_addedIngredients.Count == 1 && boilingLoopSound != null && !_loopSource.isPlaying)
            {
                _loopSource.clip = boilingLoopSound;
                _loopSource.time = 0f;
                _loopSource.Play();
            }

            Debug.Log($"[Cauldron] Ingredient ajoute : {ingName} ({type}). Total : {_addedIngredients.Count}");
            onIngredientAdded?.Invoke(ingName);
        }

        // ------------------------------------------------------------------
        //  Brassage (declenche par StirDetector)
        // ------------------------------------------------------------------
        public void HandleStirComplete()
        {
            if (_brewing) return;
            if (_addedIngredients.Count == 0)
            {
                Debug.Log("[Cauldron] Brassage : chaudron vide.");
                return;
            }

            StartCoroutine(BrewRoutine());
        }

        private IEnumerator BrewRoutine()
        {
            _brewing = true;

            // Petite pause theatrale
            yield return new WaitForSeconds(0.2f);

            PotionRecipe matched = FindMatchingRecipe();

            if (matched != null)
            {
                Debug.Log($"[Cauldron] Recette trouvee : {matched.resultPotionName}!");
                yield return StartCoroutine(BrewSuccess(matched));
            }
            else
            {
                Debug.Log("[Cauldron] Aucune recette ne correspond — echec.");
                yield return StartCoroutine(BrewFail());
            }

            _brewing = false;
        }

        private IEnumerator BrewSuccess(PotionRecipe recipe)
        {
            // VFX de brassage
            if (recipe.brewVFXPrefab != null)
            {
                Vector3 spawnPos = potionSpawnPoint != null
                    ? potionSpawnPoint.position
                    : transform.position + Vector3.up * 0.3f;

                GameObject vfx = Instantiate(recipe.brewVFXPrefab, spawnPos, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            _loopSource.Stop();

            if (audioSource != null && brewSuccessSound != null)
                audioSource.PlayOneShot(brewSuccessSound, brewSuccessSoundVolume);
            else if (audioSource != null && recipe.brewSound != null)
                audioSource.PlayOneShot(recipe.brewSound);

            // Change la couleur du liquide
            SetLiquidColor(recipe.resultColor);

            yield return new WaitForSeconds(0.1f);

            // Spawn la fiole/potion
            if (recipe.resultPrefab != null)
            {
                Vector3 spawnPos = potionSpawnPoint != null
                    ? potionSpawnPoint.position
                    : new Vector3(-1.423f, 1.768f, -0.114f);

                GameObject potionGO = Instantiate(recipe.resultPrefab, spawnPos, recipe.resultPrefab.transform.rotation);
                potionGO.AddComponent<CraftedPotion>().recipe = recipe;
                PotionResult potionResult = potionGO.GetComponent<PotionResult>();
                if (potionResult != null)
                    potionResult.Initialize(recipe);

                // Desactive la physique jusqu'a ce que le joueur recupere la potion
                Rigidbody rb = potionGO.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    XRGrabInteractable grab = potionGO.GetComponent<XRGrabInteractable>();
                    if (grab != null)
                        grab.selectExited.AddListener(_ => StartCoroutine(EnableGravityNextFrame(rb)));
                }
            }

            onPotionBrewed?.Invoke(recipe);

            // Reinitialise apres un delai
            yield return new WaitForSeconds(2f);
            ResetCauldron();
        }

        private IEnumerator BrewFail()
        {
            _loopSource.Stop();
            PlaySound(recipeFailSound);

            // Clignote rouge pour indiquer l'echec
            SetLiquidColor(Color.red);
            yield return new WaitForSeconds(0.4f);
            SetLiquidColor(_currentLiquidColor);
            yield return new WaitForSeconds(0.3f);
            SetLiquidColor(Color.red);
            yield return new WaitForSeconds(0.4f);

            onBrewFailed?.Invoke();

            yield return new WaitForSeconds(1f);
            ResetCauldron();
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------
private PotionRecipe FindMatchingRecipe()
        {
            if (recipes == null) return null;
            foreach (PotionRecipe recipe in recipes)
            {
                if (recipe != null && recipe.Matches(_addedIngredients))
                    return recipe;
            }
            return null;
        }

        private void ResetCauldron()
        {
            _addedIngredients.Clear();
            _currentLiquidColor = Color.clear;
            SetLiquidColor(_defaultLiquidColor);
            onCauldronReset?.Invoke();
            Debug.Log("[Cauldron] Reinitialise.");
        }

        private void BlendLiquidColor(Color ingredientColor)
        {
            if (_addedIngredients.Count == 1)
                _currentLiquidColor = ingredientColor;
            else
                _currentLiquidColor = Color.Lerp(_currentLiquidColor, ingredientColor, 0.5f);

            SetLiquidColor(_currentLiquidColor);
        }

        private void SetLiquidColor(Color color)
        {
            if (cauldronLiquidRenderer == null) return;
            Material[] mats = cauldronLiquidRenderer.materials;
            if (liquidMaterialIndex >= mats.Length) return;
            if (mats[liquidMaterialIndex].HasProperty(liquidColorProperty))
                mats[liquidMaterialIndex].SetColor(liquidColorProperty, color);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private IEnumerator EnableGravityNextFrame(Rigidbody rb)
        {
            yield return null;
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        // ------------------------------------------------------------------
        //  Gizmos
        // ------------------------------------------------------------------
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.3f);
            Collider col = GetComponent<Collider>();
            if (col != null)
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}
