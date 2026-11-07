using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Something Is Missing/Game Config", fileName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    public List<IngredientData> ingredients = new();
    [Tooltip("Can be put on the plate like an ingredient, but never part of a recipe. Serving it ends the run.")]
    public IngredientData fork;
    [Tooltip("Ingredient count of each dish in a run, in order.")]
    public List<int> recipeLengths = new() { 3, 4, 5 };
    [Tooltip("Seconds from serving to the wrong/correct result (eating time).")]
    [Min(0f)] public float resolveDelay = 1.4f;
    [Tooltip("Seconds after a correct dish's line is fully shown until the next dish or the final.")]
    [Min(0f)] public float nextDishDelay = 1.5f;

    void OnValidate()
    {
        for (int i = 0; i < recipeLengths.Count; i++)
        {
            recipeLengths[i] = Mathf.Max(1, recipeLengths[i]);
            if (recipeLengths[i] > ingredients.Count)
                Debug.LogWarning($"{name}: recipe length {recipeLengths[i]} exceeds ingredient count {ingredients.Count}.", this);
        }
    }
}
