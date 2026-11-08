using UnityEngine;

// Horizontal dark lines, like an old CRT screen.
public class ScanlineEffect : ScreenEffect
{
    [Tooltip("Screen pixels from one line to the next.")]
    [Range(2f, 16f)]
    [SerializeField] float spacing = 4f;
    [Tooltip("Share of each step that is dark.")]
    [Range(0f, 1f)]
    [SerializeField] float thickness = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] float intensity = 0.25f;
    [Tooltip("Lines per second the pattern drifts down. 0 = still.")]
    [Range(0f, 10f)]
    [SerializeField] float scrollSpeed = 1f;

    static readonly int Spacing = Shader.PropertyToID("_Spacing");
    static readonly int Thickness = Shader.PropertyToID("_Thickness");
    static readonly int Intensity = Shader.PropertyToID("_Intensity");
    static readonly int ScrollSpeed = Shader.PropertyToID("_ScrollSpeed");

    protected override string ShaderName => "Hidden/Something Is Missing/Screen Scanline";

    protected override void Apply(Material material)
    {
        material.SetFloat(Spacing, spacing);
        material.SetFloat(Thickness, thickness);
        material.SetFloat(Intensity, intensity);
        material.SetFloat(ScrollSpeed, scrollSpeed);
    }
}
