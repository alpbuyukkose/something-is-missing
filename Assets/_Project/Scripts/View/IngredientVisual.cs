using UnityEngine;

public static class IngredientVisual
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static MaterialPropertyBlock block;

    public static GameObject Create(IngredientData data, GameObject greyboxPrefab, Transform parent)
    {
        if (data.modelPrefab != null) return Object.Instantiate(data.modelPrefab, parent, false);
        var visual = Object.Instantiate(greyboxPrefab, parent, false);
        Tint(visual, data.greyboxColor);
        return visual;
    }

    public static void Tint(GameObject target, Color color)
    {
        block ??= new MaterialPropertyBlock();
        foreach (var renderer in target.GetComponentsInChildren<Renderer>())
        {
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, color);
            renderer.SetPropertyBlock(block);
        }
    }
}
