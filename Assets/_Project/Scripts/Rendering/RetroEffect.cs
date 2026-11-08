using System.Collections.Generic;
using UnityEngine;

// Low resolution, then posterize or map to a palette. Dither only shows in the darkest and brightest areas.
public class RetroEffect : ScreenEffect
{
    public enum ColorMode { Posterize, Palette }
    public enum PosterizeSpace { Lightness, RGB }
    // Least to most visible (the Inspector lists by value). Values are saved in the scene.
    public enum DitherPattern
    {
        InterleavedGradient = 1,
        Bayer8x8 = 3,
        Bayer4x4 = 4,
        Checker = 5,
        WhiteNoise = 6,
        Bayer2x2 = 7,
        HorizontalLines = 8,
        DiagonalLines = 9,
        Crosshatch = 10,
        HalftoneDots = 11,
    }

    const int MaxColors = 32;

    [Header("Pixels")]
    [Tooltip("Height of the virtual screen in pixels. 0 = off. Clean on 1080p: 540, 360, 270.")]
    [Min(0)]
    [SerializeField] int pixelHeight = 540;

    [Header("Before Colors")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] float brightness;
    [Range(0f, 2f)]
    [SerializeField] float contrast = 1f;
    [Range(0f, 2f)]
    [SerializeField] float saturation = 1f;

    [Header("Colors")]
    [SerializeField] ColorMode mode = ColorMode.Palette;
    [Tooltip("Posterize: number of steps. 0 = off.")]
    [Range(0, 32)]
    [SerializeField] int levels = 8;
    [Tooltip("Posterize. Lightness: only brightness is stepped, hues stay (no rainbow bands). RGB: each channel is stepped on its own.")]
    [SerializeField] PosterizeSpace posterizeSpace = PosterizeSpace.Lightness;
    [Tooltip("Palette: every pixel becomes the closest of these colors.")]
    [SerializeField] ScreenPalette palette;
    [Tooltip("Palette: added to every palette, so things like the feedback green/yellow stay readable.")]
    [SerializeField] List<Color> keepColors = new()
    {
        new(0.25f, 0.68f, 0.29f),
        new(0.95f, 0.77f, 0.06f),
    };
    [Tooltip("A Keep Color is only used for pixels this close to it, so it does not leak into the rest of the image.")]
    [Range(0f, 0.5f)]
    [SerializeField] float keepTolerance = 0.12f;

    [Header("Dither")]
    [Range(0f, 1f)]
    [SerializeField] float ditherStrength = 1f;
    [Tooltip("Brightness below which dither fades in, strongest at black. 0 = no dither in the darks.")]
    [Range(0f, 1f)]
    [SerializeField] float shadowRange = 0.25f;
    [Tooltip("Brightness range from white down where dither fades in. 0 = no dither in the lights.")]
    [Range(0f, 1f)]
    [SerializeField] float highlightRange = 0.15f;
    [Tooltip("From least to most visible: fine noise, ordered grids, then lines, hatching and print dots.")]
    [SerializeField] DitherPattern ditherPattern = DitherPattern.Bayer4x4;

    static readonly int PixelHeight = Shader.PropertyToID("_PixelHeight");
    static readonly int Brightness = Shader.PropertyToID("_Brightness");
    static readonly int Contrast = Shader.PropertyToID("_Contrast");
    static readonly int Saturation = Shader.PropertyToID("_Saturation");
    static readonly int Mode = Shader.PropertyToID("_Mode");
    static readonly int Levels = Shader.PropertyToID("_Levels");
    static readonly int Space = Shader.PropertyToID("_PosterizeSpace");
    static readonly int PaletteCount = Shader.PropertyToID("_PaletteCount");
    static readonly int KeepStart = Shader.PropertyToID("_KeepStart");
    static readonly int KeepTolerance = Shader.PropertyToID("_KeepTolerance");
    static readonly int PaletteRgb = Shader.PropertyToID("_PaletteRgb");
    static readonly int PaletteLab = Shader.PropertyToID("_PaletteLab");
    static readonly int DitherStrength = Shader.PropertyToID("_DitherStrength");
    static readonly int ShadowRange = Shader.PropertyToID("_ShadowRange");
    static readonly int HighlightRange = Shader.PropertyToID("_HighlightRange");
    static readonly int DitherPatternId = Shader.PropertyToID("_DitherPattern");

    readonly Vector4[] rgb = new Vector4[MaxColors];
    readonly Vector4[] lab = new Vector4[MaxColors];

    protected override string ShaderName => "Hidden/Something Is Missing/Screen Retro";

    protected override void Apply(Material material)
    {
        material.SetFloat(PixelHeight, pixelHeight);
        material.SetFloat(Brightness, brightness);
        material.SetFloat(Contrast, contrast);
        material.SetFloat(Saturation, saturation);
        material.SetFloat(Levels, levels);
        material.SetFloat(Space, (int)posterizeSpace);
        material.SetFloat(DitherStrength, ditherStrength);
        material.SetFloat(ShadowRange, shadowRange);
        material.SetFloat(HighlightRange, highlightRange);
        material.SetFloat(DitherPatternId, (int)ditherPattern);

        int count = 0, keepStart = 0;
        if (mode == ColorMode.Palette)
        {
            if (palette != null) foreach (var c in palette.colors) Add(c, ref count);
            keepStart = count;
            foreach (var c in keepColors) Add(c, ref count);
        }
        material.SetFloat(KeepStart, keepStart);
        material.SetFloat(KeepTolerance, keepTolerance);
        material.SetFloat(Mode, keepStart > 0 ? 1 : 0); // no palette colors = posterize
        material.SetFloat(PaletteCount, count);
        material.SetVectorArray(PaletteRgb, rgb);
        material.SetVectorArray(PaletteLab, lab);
    }

    void Add(Color c, ref int count)
    {
        if (count >= MaxColors) return;
        Color linear = c.linear;
        rgb[count] = new Vector4(linear.r, linear.g, linear.b, 1f);
        lab[count] = Oklab(linear);
        count++;
    }

    // Perceptual color space: distances here match how different colors look.
    static Vector4 Oklab(Color c)
    {
        float l = Mathf.Pow(0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b, 1f / 3f);
        float m = Mathf.Pow(0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b, 1f / 3f);
        float s = Mathf.Pow(0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b, 1f / 3f);
        return new Vector4(
            0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
            1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
            0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s, 0f);
    }
}
