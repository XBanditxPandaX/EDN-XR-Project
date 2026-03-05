// PotionRecipe.cs
// ScriptableObject qui definit une recette de potion.
// Creez une instance via Assets > Create > PotionClassroom > PotionRecipe.
//
// Une recette est validee si le chaudron contient EXACTEMENT les ingredients
// specifies (sans plus ni moins) ET que le joueur a touillee.
//
// Exemple de recette "Potion de Soin":
//   requiredIngredients : [PotionBottle, Pumpkin]
//   resultPotionName    : "Potion de Soin"
//   resultColor         : rouge
//   resultPrefab        : prefab de la fiole rouge

using UnityEngine;

namespace PotionClassroom
{
    [CreateAssetMenu(
        fileName = "NewPotionRecipe",
        menuName  = "PotionClassroom/PotionRecipe")]
    public class PotionRecipe : ScriptableObject
    {
        [Header("Ingredients requis")]
        [Tooltip("Liste des types d'ingredients necessaires (l'ordre n'a pas d'importance).")]
        public IngredientType[] requiredIngredients;

        [Header("Resultat")]
        [Tooltip("Nom de la potion obtenue.")]
        public string resultPotionName = "Potion Inconnue";

        [Tooltip("Description courte affichee dans l'UI.")]
        [TextArea(2, 4)]
        public string resultDescription = "";

        [Tooltip("Couleur de la potion resultante (pour teinter le liquide du chaudron ou la fiole).")]
        public Color resultColor = Color.magenta;

        [Tooltip("Icone de la potion pour l'UI (optionnel).")]
        public Sprite resultIcon;

        [Tooltip("Prefab de la fiole/objet potion qui apparait dans le chaudron (optionnel).")]
        public GameObject resultPrefab;

        [Header("Effets")]
        [Tooltip("Son joue quand la potion est creee (optionnel).")]
        public AudioClip brewSound;

        [Tooltip("Particules jouees dans le chaudron apres brassage (optionnel).")]
        public GameObject brewVFXPrefab;

        // ------------------------------------------------------------------
        //  Methode de validation
        // ------------------------------------------------------------------
        /// <summary>
        /// Verifie si la liste d'ingredients donnee correspond exactement a cette recette.
        /// L'ordre n'a pas d'importance, mais le compte exact de chaque type est verifie.
        /// </summary>
        public bool Matches(System.Collections.Generic.List<IngredientType> ingredients)
        {
            if (requiredIngredients == null || requiredIngredients.Length == 0) return false;
            if (ingredients.Count != requiredIngredients.Length) return false;

            // Copie pour pouvoir "consommer" les elements de la liste
            var remaining = new System.Collections.Generic.List<IngredientType>(ingredients);

            foreach (IngredientType required in requiredIngredients)
            {
                if (!remaining.Remove(required))
                    return false; // Cet ingredient requis est absent
            }

            return remaining.Count == 0;
        }
    }
}
