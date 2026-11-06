using UnityEngine;

[CreateAssetMenu(menuName = "Something Is Missing/Ingredient", fileName = "Ingredient")]
public class IngredientData : ScriptableObject
{
    public string displayName;
    public Color greyboxColor = Color.white;
    [Tooltip("Optional, pivot at the bottom. Empty = tinted greybox cube.")]
    public GameObject modelPrefab;
}
