using UnityEngine;

// Flickering noise, like old film. Placed before RetroEffect it takes the big pixels' size.
public class FilmGrainEffect : ScreenEffect
{
    // Least to most visible (the Inspector lists by value). Values are saved in the scene.
    public enum GrainType
    {
        Soft = 0,
        Fine = 1,
        Colored = 2,
        Coarse = 3,
        Streaks = 4,
    }

    [Tooltip("Soft: gentle. Fine: sharp per pixel. Colored: separate per channel. Coarse: blotchy clumps. Streaks: horizontal, like a worn tape.")]
    [SerializeField] GrainType type = GrainType.Fine;
    [Range(0f, 0.5f)]
    [SerializeField] float intensity = 0.06f;
    [Tooltip("New noise patterns per second.")]
    [Range(1f, 60f)]
    [SerializeField] float framesPerSecond = 24f;
    [Tooltip("1 = dark areas get less grain and black stays black. 0 = even everywhere.")]
    [Range(0f, 1f)]
    [SerializeField] float shadowFade = 1f;

    static readonly int Type = Shader.PropertyToID("_GrainType");
    static readonly int Intensity = Shader.PropertyToID("_Intensity");
    static readonly int Fps = Shader.PropertyToID("_Fps");
    static readonly int ShadowFade = Shader.PropertyToID("_ShadowFade");

    protected override string ShaderName => "Hidden/Something Is Missing/Screen Film Grain";

    protected override void Apply(Material material)
    {
        material.SetFloat(Type, (int)type);
        material.SetFloat(Intensity, intensity);
        material.SetFloat(Fps, framesPerSecond);
        material.SetFloat(ShadowFade, shadowFade);
    }
}
