using UnityEngine;

public static class IngredientVisual
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static MaterialPropertyBlock block;

    public static GameObject Create(IngredientData data, GameObject greyboxPrefab, Transform parent)
    {
        if (data.modelPrefab != null)
        {
            var model = Object.Instantiate(data.modelPrefab, parent, false);
            // Models may come straight from a scene file with their placement baked in; keep only the scale.
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (data.tintModel) Tint(model, data.greyboxColor);
            return model;
        }
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
