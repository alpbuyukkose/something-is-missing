using UnityEngine;

// Darkens the edges of the screen.
public class VignetteEffect : ScreenEffect
{
    [Range(0f, 1f)]
    [SerializeField] float intensity = 0.6f;
    [Tooltip("Distance from the center where darkening starts (0.5 = screen edge).")]
    [Range(0f, 1f)]
    [SerializeField] float radius = 0.75f;
    [Tooltip("Width of the fade into the dark.")]
    [Range(0.01f, 1f)]
    [SerializeField] float softness = 0.45f;
    [Tooltip("1 = round on a wide screen, 0 = follows the screen's shape.")]
    [Range(0f, 1f)]
    [SerializeField] float roundness = 1f;
    [SerializeField] Color color = Color.black;

    static readonly int Intensity = Shader.PropertyToID("_Intensity");
    static readonly int Radius = Shader.PropertyToID("_Radius");
    static readonly int Softness = Shader.PropertyToID("_Softness");
    static readonly int Roundness = Shader.PropertyToID("_Roundness");
    static readonly int VignetteColor = Shader.PropertyToID("_VignetteColor");

    protected override string ShaderName => "Hidden/Something Is Missing/Screen Vignette";

    protected override void Apply(Material material)
    {
        material.SetFloat(Intensity, intensity);
        material.SetFloat(Radius, radius);
        material.SetFloat(Softness, softness);
        material.SetFloat(Roundness, roundness);
        material.SetColor(VignetteColor, color);
    }
}
