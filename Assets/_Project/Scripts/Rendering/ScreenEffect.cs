using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// One full-screen effect. While this component is enabled, ScreenEffectsFeature draws it on the game camera,
// lowest Order first. Runs in edit mode too, so the Game view previews it.
[ExecuteAlways]
public abstract class ScreenEffect : MonoBehaviour
{
    static readonly List<ScreenEffect> active = new();

    // Master switch, set by ScreenEffectsManager.
    public static bool AllEnabled { get; set; } = true;
    public static IReadOnlyList<ScreenEffect> Active => active;

    [Tooltip("Lower runs first.")]
    [SerializeField] int order;
    [SerializeField] Shader shader;

    Material material;

    // The effect's material with its current settings, or null if the shader is missing.
    public Material Material
    {
        get
        {
            if (material == null && shader != null) material = CoreUtils.CreateEngineMaterial(shader);
            if (material != null) Apply(material);
            return material;
        }
    }

    protected abstract string ShaderName { get; }
    protected abstract void Apply(Material material);

    void Reset() => shader = Shader.Find(ShaderName);

    protected virtual void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
        Sort();
    }

    protected virtual void OnDisable() => active.Remove(this);

    void OnDestroy() => CoreUtils.Destroy(material);

    void OnValidate() => Sort();

    static void Sort() => active.Sort((a, b) => a.order.CompareTo(b.order));
}
