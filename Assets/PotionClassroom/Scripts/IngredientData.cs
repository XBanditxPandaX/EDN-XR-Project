// IngredientData.cs
// ScriptableObject qui decrit un ingredient.
// Creez une instance via le menu Assets > Create > PotionClassroom > IngredientData
// et assignez-la au composant Ingredient sur chaque prefab d'ingredient.

using UnityEngine;

namespace PotionClassroom
{
    [CreateAssetMenu(
        fileName = "NewIngredientData",
        menuName  = "PotionClassroom/IngredientData")]
    public class IngredientData : ScriptableObject
    {
        [Header("Identite")]
        [Tooltip("Type unique de cet ingredient (doit correspondre aux recettes).")]
        public IngredientType ingredientType = IngredientType.None;

        [Tooltip("Nom affiche dans l'UI (ex: 'Citrouille', 'Oeil de Verre').")]
        public string displayName = "Ingredient";

        [Tooltip("Icone optionnelle pour l'UI.")]
        public Sprite icon;

        [Header("Visuels dans le chaudron")]
        [Tooltip("Couleur emise par le liquide du chaudron quand cet ingredient est ajoute.")]
        public Color cauldronTintColor = Color.green;

        [Header("Comportement")]
        [Tooltip("Si true, l'ingredient est detruit apres avoir ete mis dans le chaudron.")]
        public bool destroyOnAdd = true;

        [Tooltip("L'etagere respawn un nouvel ingredient apres ce delai (secondes). 0 = jamais.")]
        [Min(0f)]
        public float respawnDelay = 5f;
    }
}
