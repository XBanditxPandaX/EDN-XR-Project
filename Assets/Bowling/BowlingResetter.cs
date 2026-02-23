using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;     // Pour InputActionReference
using UnityEngine.SceneManagement; // Pour SceneManager

public class BowlingResetter : MonoBehaviour
{
    public InputActionReference resetAction; // Bouton de réinitialisation

    void Update()
    {
        // Vérifie si le bouton est déclenché
        if (resetAction != null && resetAction.action.triggered)
        {
            // Récupère le nom de la scène actuelle
            string currentSceneName = SceneManager.GetActiveScene().name;

            // Recharge la scène
            SceneManager.LoadScene(currentSceneName);
        }
    }
}