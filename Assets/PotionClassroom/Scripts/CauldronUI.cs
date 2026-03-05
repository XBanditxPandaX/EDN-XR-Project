// CauldronUI.cs
// Affiche en temps reel le contenu du chaudron et le resultat de la potion
// via des elements TextMeshPro sur un Canvas World Space.
//
// Comment configurer :
//  1. Ajoutez un Canvas (World Space) au-dessus du chaudron.
//  2. Ajoutez des TextMeshProUGUI pour la liste d'ingredients et le nom de la potion.
//  3. Assignez ce script sur le chaudron (ou le Canvas) et reliez les references.
//  4. Reliez les evenements du Cauldron aux methodes de ce script via l'Inspector :
//       - Cauldron.onIngredientAdded  => CauldronUI.OnIngredientAdded
//       - Cauldron.onPotionBrewed     => CauldronUI.OnPotionBrewed
//       - Cauldron.onBrewFailed       => CauldronUI.OnBrewFailed
//       - Cauldron.onCauldronReset    => CauldronUI.OnCauldronReset

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace PotionClassroom
{
    public class CauldronUI : MonoBehaviour
    {
        // ------------------------------------------------------------------
        //  Inspector
        // ------------------------------------------------------------------
        [Header("Textes UI")]
        [Tooltip("Texte affichant la liste des ingredients dans le chaudron.")]
        public TextMeshProUGUI ingredientsText;

        [Tooltip("Texte affichant le resultat (nom de la potion ou message d'echec).")]
        public TextMeshProUGUI resultText;

        [Tooltip("Texte affichant un hint 'Brassez pour obtenir une potion'.")]
        public TextMeshProUGUI hintText;

        [Header("Couleurs")]
        public Color defaultIngredientColor = Color.white;
        public Color successColor           = Color.green;
        public Color failColor              = Color.red;
        public Color hintColor              = Color.yellow;

        [Header("Duree d'affichage du resultat (secondes)")]
        [Min(0.5f)]
        public float resultDisplayDuration = 4f;

        // ------------------------------------------------------------------
        //  Etat interne
        // ------------------------------------------------------------------
        private readonly List<string> _ingredientNames = new List<string>();
        private Coroutine _clearResultCoroutine;

        // ------------------------------------------------------------------
        //  Unity lifecycle
        // ------------------------------------------------------------------
        private void Start()
        {
            ClearAll();
        }

        // ------------------------------------------------------------------
        //  API publique (reliee aux evenements Cauldron)
        // ------------------------------------------------------------------
        /// <summary>
        /// Appele par Cauldron.onIngredientAdded (string = nom de l'ingredient).
        /// </summary>
        public void OnIngredientAdded(string ingredientName)
        {
            _ingredientNames.Add(ingredientName);
            RefreshIngredientList();

            if (hintText != null)
            {
                hintText.text  = "Brassez pour creer une potion !";
                hintText.color = hintColor;
                hintText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Appele par Cauldron.onPotionBrewed (PotionRecipe = recette resultante).
        /// </summary>
        public void OnPotionBrewed(PotionRecipe recipe)
        {
            ShowResult(
                recipe != null ? $"{recipe.resultPotionName}" : "Potion inconnue",
                recipe != null ? recipe.resultDescription : "",
                successColor);

            _ingredientNames.Clear();
            RefreshIngredientList();
        }

        /// <summary>
        /// Appele par Cauldron.onBrewFailed.
        /// </summary>
        public void OnBrewFailed()
        {
            ShowResult("Combinaison ratee !", "Essayez d'autres ingredients.", failColor);

            _ingredientNames.Clear();
            RefreshIngredientList();
        }

        /// <summary>
        /// Appele par Cauldron.onCauldronReset.
        /// </summary>
        public void OnCauldronReset()
        {
            _ingredientNames.Clear();
            RefreshIngredientList();
            HideHint();
        }

        // ------------------------------------------------------------------
        //  Helpers
        // ------------------------------------------------------------------
        private void RefreshIngredientList()
        {
            if (ingredientsText == null) return;

            if (_ingredientNames.Count == 0)
            {
                ingredientsText.text  = "Chaudron vide";
                ingredientsText.color = new Color(1f, 1f, 1f, 0.4f);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("<b>Ingredients :</b>");
            foreach (string name in _ingredientNames)
                sb.AppendLine($"  • {name}");

            ingredientsText.text  = sb.ToString().TrimEnd();
            ingredientsText.color = defaultIngredientColor;
        }

        private void ShowResult(string title, string description, Color color)
        {
            if (resultText == null) return;

            if (_clearResultCoroutine != null)
                StopCoroutine(_clearResultCoroutine);

            resultText.text  = string.IsNullOrEmpty(description)
                ? $"<b>{title}</b>"
                : $"<b>{title}</b>\n{description}";
            resultText.color = color;
            resultText.gameObject.SetActive(true);

            _clearResultCoroutine = StartCoroutine(ClearResultAfterDelay());
        }

        private IEnumerator ClearResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDisplayDuration);
            if (resultText != null)
            {
                resultText.text = "";
                resultText.gameObject.SetActive(false);
            }
            HideHint();
            _clearResultCoroutine = null;
        }

        private void ClearAll()
        {
            _ingredientNames.Clear();
            RefreshIngredientList();

            if (resultText != null)
            {
                resultText.text = "";
                resultText.gameObject.SetActive(false);
            }

            HideHint();
        }

        private void HideHint()
        {
            if (hintText != null)
                hintText.gameObject.SetActive(false);
        }
    }
}
