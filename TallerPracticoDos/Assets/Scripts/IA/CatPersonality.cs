using UnityEngine;

[System.Serializable]
public class CatPersonality : MonoBehaviour
{
    [Range(0f, 2f)] public float curiosity = 1f;
    [Range(0f, 2f)] public float aggression = 1f;
    [Range(0f, 2f)] public float affection = 1f;

    [Tooltip("Nombre de objeto favorito (opcional, ej: 'ChristmasTree')")]
    public string favoriteObjectName = "";

    public float PersonalityMultiplier(string goalName)
    {
        return goalName switch
        {
            "Explore" => curiosity,
            "DestroyObject" => aggression,
            "PlayWithPlayer" => affection,
            _ => 1f
        };
    }
}
