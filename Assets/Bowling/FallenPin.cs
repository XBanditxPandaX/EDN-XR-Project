using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallenPin : MonoBehaviour
{
    public float fallAngleThreshold = 45.0f;
    public bool isFallen = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // On vérifie uniquement si la quille n'est pas déjà tombée
        if (!isFallen)
        {
            // Calcul de l’angle entre l’axe local de la quille et le haut du monde
            float angle = Vector3.Angle(transform.up, Vector3.up);

            // Si l’angle dépasse le seuil, la quille est considérée comme tombée
            if (angle > fallAngleThreshold)
            {
                isFallen = true;
                Debug.Log(gameObject.name + " est tombée !");
            }
        }
    }
}
