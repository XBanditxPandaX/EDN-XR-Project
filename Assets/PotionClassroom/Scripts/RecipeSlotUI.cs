using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PotionClassroom
{
    public class RecipeSlotUI : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Texte du nom de la potion.")]
        public TextMeshProUGUI potionNameText;

        [Tooltip("Texte de la liste des ingredients.")]
        public TextMeshProUGUI ingredientsText;

        [Tooltip("Image coloree representant la potion.")]
        public Image colorIndicator;

        [Tooltip("Icone de validation (coche) — desactive par defaut.")]
        public GameObject checkmark;

        // ------------------------------------------------------------------
        public PotionRecipe AssignedRecipe { get; private set; }
        public bool IsFulfilled           { get; private set; }

        // ------------------------------------------------------------------
        public void SetRecipe(PotionRecipe recipe)
        {
            AssignedRecipe = recipe;
            IsFulfilled    = false;

            if (potionNameText != null)
                potionNameText.text = recipe.resultPotionName;

            if (ingredientsText != null)
            {
                string list = "";
                foreach (IngredientType t in recipe.requiredIngredients)
                    list += "• " + GetIngredientName(t) + "\n";
                ingredientsText.text = list.TrimEnd();
            }

            if (colorIndicator != null)
                colorIndicator.color = recipe.resultColor;

            if (checkmark != null)
                checkmark.SetActive(false);
        }

        public void SetFulfilled(bool fulfilled)
        {
            IsFulfilled = fulfilled;
            if (checkmark != null)
                checkmark.SetActive(fulfilled);
        }

        // ------------------------------------------------------------------
        private static string GetIngredientName(IngredientType type) => type switch
        {
            IngredientType.PotionBottle   => "Salive de potion",
            IngredientType.Pumpkin        => "Citrouille",
            IngredientType.Eye            => "Oeil d'humain",
            IngredientType.MegalodonTooth => "Dent de requin",
            IngredientType.FireDemonFruit => "Fruit du demon",
            IngredientType.Calcifer       => "Calcifer",
            _                             => type.ToString(),
        };
    }
}
