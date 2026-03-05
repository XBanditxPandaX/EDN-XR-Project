// IngredientType.cs
// Enumeration de tous les types d'ingredients disponibles dans la classe de potions.
// Chaque etagere est associee a un seul type. Ajoutez de nouveaux types ici si vous
// ajoutez de nouveaux assets.

namespace PotionClassroom
{
    public enum IngredientType
    {
        None = 0,
        PotionBottle      = 1,   // role-playing-game-potions (HPRegenBottle)
        Pumpkin           = 2,   // pumpkin-grenade
        Eye               = 3,   // procedural-eye
        MegalodonTooth    = 4,   // megalodon-teeth-fossil
        FireDemonFruit    = 5,   // one-piece-mera-mera-no-mi
        Calcifer          = 6,   // calcifer (fire spirit)
    }
}
