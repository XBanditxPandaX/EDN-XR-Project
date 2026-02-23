using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Important pour TextMeshPro

public class PinCounter : MonoBehaviour
{
    FallenPin[] pins;
    int fallenCount = 0;

    public TextMeshProUGUI scoreText; // Référence au texte UI

    void Start()
    {
        pins = GetComponentsInChildren<FallenPin>();
    }

    void Update()
    {
        fallenCount = 0;

        foreach (FallenPin pin in pins)
        {
            if (pin.isFallen)
            {
                fallenCount++;
            }
        }

        // Mise à jour du texte UI
        scoreText.text = fallenCount.ToString();
    }
}