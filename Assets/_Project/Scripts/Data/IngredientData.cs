using UnityEngine;

[CreateAssetMenu(menuName = "Something Is Missing/Ingredient", fileName = "Ingredient")]
public class IngredientData : ScriptableObject
{
    public string displayName;
    public Color greyboxColor = Color.white;
    [Tooltip("Optional. Empty = tinted greybox cube. If Pantry's Model Spots has a child with the same name, the ingredient rests at that child's pose.")]
    public GameObject modelPrefab;
    [Tooltip("Tint the model with the greybox color (useful until it has textures).")]
    public bool tintModel = true;
}
