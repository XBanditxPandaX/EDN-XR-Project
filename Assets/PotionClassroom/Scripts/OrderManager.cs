using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PotionClassroom
{
    [System.Serializable]
    public class IngredientCanvasEntry
    {
        public IngredientType ingredientType;
        [Tooltip("Canvas avec le RawImage de cet ingredient (reste cache, sert de template).")]
        public GameObject canvasTemplate;
    }

    [System.Serializable]
    public class PotionCanvasEntry
    {
        public PotionRecipe recipe;
        [Tooltip("Canvas avec le RawImage de cette potion (reste cache, sert de template).")]
        public GameObject canvasTemplate;
    }

    public class OrderManager : MonoBehaviour
    {
        [Header("Templates - ingredients")]
        [Tooltip("Un entry par ingredient : son type + son Canvas RawImage.")]
        public IngredientCanvasEntry[] ingredientCanvases;

        [Header("Templates - potions")]
        [Tooltip("Un entry par potion : sa recette + son Canvas RawImage.")]
        public PotionCanvasEntry[] potionCanvases;

        [Header("Affichage")]
        [Tooltip("Parent ou afficher les lignes (Torah_S).")]
        public Transform displayParent;

        [Tooltip("Position locale du premier element (haut-gauche de la grille).")]
        public Vector3 startLocalPosition = new Vector3(-0.24f, 0.1f, 0f);

        [Tooltip("Espacement horizontal entre les 5 colonnes.")]
        public float columnSpacing = 0.12f;

        [Tooltip("Espacement vertical entre les lignes (negatif = vers le bas).")]
        public float rowSpacing = -0.15f;

        [Tooltip("Correction horizontale par ligne (positif = droite). Ajuste si les lignes derivent.")]
        public float rowXOffset = 0f;

        [Tooltip("Correction de Z par ligne vers le haut (positif si les lignes du haut flottent devant le parchemin).")]
        public float rowZStep = 0f;

        [Tooltip("Taille des separateurs + et = en unites world.")]
        public float separatorSize = 0.05f;

        [Header("References")]
        public ChestController chest;
        public Button confirmButton;
        public GameManager gameManager;

        [Header("Demarrage")]
        [Tooltip("Genere une commande des que l'OrderManager demarre.")]
        public bool generateOrderOnStart = true;

        [Header("Parametres commande")]
        [Min(1)] public int minPotions = 1;
        [Range(1, 3)] public int maxPotions = 3;

        // ------------------------------------------------------------------
        private List<PotionRecipe> _currentOrder  = new List<PotionRecipe>();
        private List<GameObject>   _spawnedObjects = new List<GameObject>();
        private bool _isInitialized = false;
        private bool _ordersActive = false;

        // ------------------------------------------------------------------
        private void Start()
        {
            Debug.Log($"[OrderManager] displayParent = {(displayParent != null ? displayParent.name : "NULL")}");
            Debug.Log($"[OrderManager] ingredientCanvases : {ingredientCanvases?.Length ?? 0} entrees");
            Debug.Log($"[OrderManager] potionCanvases : {potionCanvases?.Length ?? 0} entrees");

            HideAllTemplates();

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);

            _isInitialized = true;

            if (generateOrderOnStart)
                BeginOrders();
            else
                PrepareForTutorial();
        }

        public void PrepareForTutorial()
        {
            generateOrderOnStart = false;
            _ordersActive = false;
            ClearDisplay();
            HideAllTemplates();
            SetConfirmButtonVisible(false);
        }

        public void BeginOrders()
        {
            generateOrderOnStart = true;
            _ordersActive = true;
            SetConfirmButtonVisible(true);

            if (_isInitialized)
                GenerateNewOrder();
        }

        public void GenerateNewOrder()
        {
            _ordersActive = true;
            SetConfirmButtonVisible(true);
            ClearDisplay();
            _currentOrder.Clear();

            // Construit le pool depuis les potionCanvases
            List<PotionRecipe> pool = new List<PotionRecipe>();
            foreach (PotionCanvasEntry e in potionCanvases)
                if (e.recipe != null) pool.Add(e.recipe);

            int count = Random.Range(Mathf.Min(minPotions, pool.Count), Mathf.Min(maxPotions, pool.Count) + 1);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                _currentOrder.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            Debug.Log($"[OrderManager] Nouvelle commande : {_currentOrder.Count} potion(s).");
            foreach (PotionRecipe r in _currentOrder)
                Debug.Log($"[OrderManager]  -> {r.resultPotionName}");
            SpawnRows();
        }

        // Appele par ChestController
        public void OnPotionDeposited(PotionRecipe recipe)
        {
            Debug.Log($"[OrderManager] Potion deposee : {recipe?.resultPotionName}");
        }

        // ------------------------------------------------------------------
        //  Affichage
        // ------------------------------------------------------------------
        private void SpawnRows()
        {
            // Rotation et scale depuis le premier template valide
            Quaternion refRot   = Quaternion.identity;
            Vector3    refScale = Vector3.one;
            foreach (IngredientCanvasEntry e in ingredientCanvases)
            {
                if (e.canvasTemplate != null)
                {
                    refRot   = e.canvasTemplate.transform.localRotation;
                    refScale = e.canvasTemplate.transform.localScale;
                    break;
                }
            }

            int count = _currentOrder.Count;
            // Centre les lignes autour de startLocalPosition.y
            float firstRowY = startLocalPosition.y - (count - 1) * rowSpacing / 2f;

            for (int row = 0; row < count; row++)
            {
                PotionRecipe recipe = _currentOrder[row];
                float y = firstRowY + row * rowSpacing;
                // Z calcule depuis l'ecart Y par rapport au centre (startLocalPosition.z = Z au centre)
                float z = rowSpacing != 0f
                    ? startLocalPosition.z + (y - startLocalPosition.y) * rowZStep / (-rowSpacing)
                    : startLocalPosition.z;

                if (recipe.requiredIngredients.Length > 0)
                    SpawnIngredient(recipe.requiredIngredients[0], Col(0, row), y, z, refRot, refScale);

                SpawnSeparator("+", Col(1, row), y, z, refRot, refScale);

                if (recipe.requiredIngredients.Length > 1)
                    SpawnIngredient(recipe.requiredIngredients[1], Col(2, row), y, z, refRot, refScale);

                SpawnSeparator("=", Col(3, row), y, z, refRot, refScale);

                SpawnPotion(recipe, Col(4, row), y, z, refRot, refScale);
            }
        }

        private float Col(int colIndex, int row = 0) => startLocalPosition.x + colIndex * columnSpacing + row * rowXOffset;

        private void SpawnIngredient(IngredientType type, float x, float y, float z, Quaternion rot, Vector3 scale)
        {
            foreach (IngredientCanvasEntry e in ingredientCanvases)
            {
                if (e.ingredientType == type && e.canvasTemplate != null)
                {
                    SpawnCopy(e.canvasTemplate, x, y, z, rot, scale);
                    return;
                }
            }
            Debug.LogWarning($"[OrderManager] Aucun canvas trouve pour l'ingredient {type}");
        }

        private void SpawnPotion(PotionRecipe recipe, float x, float y, float z, Quaternion rot, Vector3 scale)
        {
            foreach (PotionCanvasEntry e in potionCanvases)
            {
                if (e.recipe == recipe && e.canvasTemplate != null)
                {
                    SpawnCopy(e.canvasTemplate, x, y, z, rot, scale);
                    return;
                }
            }
            Debug.LogWarning($"[OrderManager] Aucun canvas trouve pour la potion {recipe?.resultPotionName}");
        }

        private void SpawnCopy(GameObject template, float x, float y, float z, Quaternion rot, Vector3 scale)
        {
            GameObject copy = Instantiate(template, displayParent);
            copy.SetActive(true);
            copy.transform.localPosition = new Vector3(x, y, z);
            copy.transform.localRotation = rot;
            copy.transform.localScale    = scale;
            _spawnedObjects.Add(copy);
        }

        private void SpawnSeparator(string text, float x, float y, float z, Quaternion rot, Vector3 scale)
        {
            // Recupere la taille du premier template pour le sizeDelta
            RectTransform refRt = null;
            foreach (IngredientCanvasEntry e in ingredientCanvases)
                if (e.canvasTemplate != null) { refRt = e.canvasTemplate.GetComponent<RectTransform>(); break; }

            GameObject go = new GameObject("Sep_" + text);
            go.transform.SetParent(displayParent);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = rot;
            go.transform.localScale    = scale;

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = refRt != null ? refRt.sizeDelta : new Vector2(100f, 100f);

            // Texte TMP UGUI centre dans le canvas
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = separatorSize;
            tmp.color     = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            _spawnedObjects.Add(go);
        }

        private void ClearDisplay()
        {
            foreach (GameObject go in _spawnedObjects)
                if (go != null) Destroy(go);
            _spawnedObjects.Clear();
        }

        private void HideAllTemplates()
        {
            foreach (IngredientCanvasEntry e in ingredientCanvases)
                if (e.canvasTemplate != null) e.canvasTemplate.SetActive(false);
            foreach (PotionCanvasEntry e in potionCanvases)
                if (e.canvasTemplate != null) e.canvasTemplate.SetActive(false);
        }

        private void SetConfirmButtonVisible(bool visible)
        {
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------
        //  Validation
        // ------------------------------------------------------------------
        private void OnConfirmClicked()
        {
            if (!_ordersActive) return;

            List<PotionRecipe> stored   = chest.GetStoredRecipes();
            List<PotionRecipe> required = new List<PotionRecipe>(_currentOrder);

            bool success = true;
            foreach (PotionRecipe r in required)
                if (!stored.Remove(r)) { success = false; break; }

            if (success)
            {
                Debug.Log("[OrderManager] Commande validee !");
                chest.ClearChest();
                gameManager?.OnOrderValidated();
                GenerateNewOrder();
            }
            else
            {
                Debug.Log("[OrderManager] Commande incomplete ou incorrecte.");
            }
        }
    }
}
