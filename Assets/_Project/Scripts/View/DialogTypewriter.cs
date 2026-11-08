using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// Reveals a line letter by letter. Write *word* in a line to show it in the emphasis color,
// and | to start a new sentence on a cleared line (e.g. "Something is still missing|*YOU*!").
[RequireComponent(typeof(TMP_Text))]
public class DialogTypewriter : MonoBehaviour
{
    public enum PunchPivot { LineCenter, EachLetter }

    [Header("Look")]
    [Tooltip("Bigger values can push long lines onto a second line.")]
    [SerializeField] float lineFontSize = 2.3f;
    [Tooltip("Resize the text box to the widest line, so left-aligned lines sit as one block around the box's pivot.")]
    [SerializeField] bool fitWidthToText;
    [SerializeField] Color textColor = new(0.847f, 0.788f, 0.659f);
    [Tooltip("Color of words written as *word*.")]
    [SerializeField] Color emphasisColor = new(0.878f, 0.271f, 0.169f);
    [Tooltip("Optional. Same text drawn behind with the glow material; each letter glows in its own color.")]
    [SerializeField] TMP_Text glowLayer;
    [Tooltip("Pixel block size in font atlas texels (the font is sampled at 90 per em). 0 = off. Needs the Dialog Text shader.")]
    [Min(0f)]
    [SerializeField] float pixelSize = 0f;

    [Header("Idle Jitter")]
    [Tooltip("Constant in-place tremble of the shown letters, in world units. 0 = off.")]
    [SerializeField] float jitterStrength = 0.004f;
    [Tooltip("New random positions per second.")]
    [SerializeField] float jitterFrequency = 12f;
    [Tooltip("On: each letter trembles on its own. Off: the line trembles as one block.")]
    [SerializeField] bool jitterLettersSeparately = true;

    [Header("Highlight (menus)")]
    [Tooltip("Extra size at full highlight (hover). 0.08 = 8% bigger.")]
    [SerializeField] float highlightScale = 0.08f;
    [Tooltip("Extra jitter at full highlight. 1.5 = 2.5x the idle jitter.")]
    [SerializeField] float highlightJitter = 1.5f;

    [Header("Reveal")]
    [Tooltip("Seconds a finished part stays before the text after | replaces it.")]
    [SerializeField] float pageHold = 0.8f;
    [SerializeField] float lettersPerSecond = 28f;
    [Tooltip("Fade-in time of a single letter. 0 = appears instantly.")]
    [SerializeField] float letterFadeDuration = 0f;

    [Header("Pauses (seconds)")]
    [SerializeField] float commaPause = 0.12f;
    [Tooltip("After . ! ? that end a sentence.")]
    [SerializeField] float sentenceEndPause = 0.4f;
    [Tooltip("After ... (no sentence end effects).")]
    [SerializeField] float ellipsisPause = 0.55f;

    [Header("Sentence End: Bounce")]
    [Tooltip("0 = off.")]
    [SerializeField] float bounceHeight = 0f;
    [SerializeField] float bounceDuration = 0.3f;
    [Tooltip("Height over time (0-1). End at 0 so the line settles in place.")]
    [SerializeField] AnimationCurve bounceCurve = new(
        new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(0.65f, -0.2f), new Keyframe(1f, 0f));

    [Header("Sentence End: Scale Punch")]
    [Tooltip("Extra size at the peak. 0.1 = 10% bigger. 0 = off.")]
    [SerializeField] float punchAmount = 0.08f;
    [SerializeField] float punchDuration = 0.22f;
    [Tooltip("Amount over time (0-1). End at 0 so the text returns to its size.")]
    [SerializeField] AnimationCurve punchCurve = new(
        new Keyframe(0f, 0f, 0f, 12f), new Keyframe(0.15f, 1f), new Keyframe(1f, 0f));
    [Tooltip("Line Center: the visible text grows as one block. Each Letter: letters grow in place.")]
    [SerializeField] PunchPivot punchPivot = PunchPivot.EachLetter;

    [Header("Sentence End: Shake")]
    [Tooltip("Max offset in world units. 0 = off.")]
    [SerializeField] float shakeStrength = 0.012f;
    [SerializeField] float shakeDuration = 0.3f;
    [Tooltip("New random offsets per second.")]
    [SerializeField] float shakeFrequency = 40f;
    [Tooltip("Strength over time (0-1).")]
    [SerializeField] AnimationCurve shakeFalloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [Tooltip("On: each letter shakes on its own. Off: the line shakes as one block.")]
    [SerializeField] bool shakeLettersSeparately = true;

    TMP_Text text;
    readonly List<bool> emphasis = new();
    readonly List<float> sentenceEndTimes = new();
    readonly List<Vector3> sentenceEndCenters = new();
    float[] revealTimes = System.Array.Empty<float>();
    TMP_MeshInfo[] textSource, glowSource;
    float startTime, lineDuration;
    string[] pages = System.Array.Empty<string>();
    int page;

    // True until the last letter of the current line is fully shown.
    // 0-1, set by MenuButton on hover.
    public float Highlight { get; set; }

    public bool IsTyping => page < pages.Length - 1 || Time.time - startTime < lineDuration;

    static readonly int PixelSizeId = Shader.PropertyToID("_PixelSize");

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        ApplyPixelSize(text);
        ApplyPixelSize(glowLayer);
        Clear();
    }

    void OnValidate()
    {
        if (TryGetComponent(out TMP_Text t))
        {
            t.fontSize = lineFontSize;
            ApplyPixelSize(t);
        }
        if (glowLayer != null)
        {
            glowLayer.fontSize = lineFontSize;
            ApplyPixelSize(glowLayer);
        }
    }

    void ApplyPixelSize(TMP_Text target)
    {
        if (target == null) return;
        var material = target.fontSharedMaterial;
        if (material != null && material.HasProperty(PixelSizeId)) material.SetFloat(PixelSizeId, pixelSize);
    }

    public void Play(string line)
    {
        pages = line.Split('|');
        ShowPage(0);
    }

    void ShowPage(int index)
    {
        page = index;
        string plain = Parse(pages[index].Trim());
        if (fitWidthToText) FitWidth(plain);
        textSource = Layout(text, plain);
        if (glowLayer != null) glowSource = Layout(glowLayer, plain);
        Schedule();
        startTime = Time.time;
        Apply();
    }

    public void Clear()
    {
        text.text = "";
        if (glowLayer != null) glowLayer.text = "";
        revealTimes = System.Array.Empty<float>();
        sentenceEndTimes.Clear();
        sentenceEndCenters.Clear();
        lineDuration = 0f;
        pages = System.Array.Empty<string>();
        page = 0;
    }

    void FitWidth(string plain)
    {
        text.fontSize = lineFontSize;
        var box = text.rectTransform;
        box.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, text.GetPreferredValues(plain, float.PositiveInfinity, float.PositiveInfinity).x + 2f);
    }

    TMP_MeshInfo[] Layout(TMP_Text target, string plain)
    {
        target.fontSize = lineFontSize;
        target.text = plain;
        target.ForceMeshUpdate();
        return target.textInfo.CopyMeshInfoVertexData();
    }

    // Strips the * markers and remembers which characters they wrapped.
    string Parse(string line)
    {
        emphasis.Clear();
        var plain = new StringBuilder(line.Length);
        bool on = false;
        foreach (char c in line)
        {
            if (c == '*') { on = !on; continue; }
            plain.Append(c);
            emphasis.Add(on);
        }
        return plain.ToString();
    }

    void Schedule()
    {
        var info = text.textInfo;
        int count = info.characterCount;
        revealTimes = new float[count];
        sentenceEndTimes.Clear();
        sentenceEndCenters.Clear();
        float t = 0f;
        lineDuration = 0f;
        for (int i = 0; i < count; i++)
        {
            revealTimes[i] = t;
            lineDuration = t + letterFadeDuration;
            t += 1f / lettersPerSecond;
            char c = info.characterInfo[i].character;
            char next = i + 1 < count ? info.characterInfo[i + 1].character : ' ';
            if (c == ',') t += commaPause;
            else if (c is '.' or '!' or '?' && next != c) // repeated marks act on the last one
            {
                bool ellipsis = c == '.' && i >= 2 && info.characterInfo[i - 1].character == '.' && info.characterInfo[i - 2].character == '.';
                if (ellipsis) t += ellipsisPause;
                else if (char.IsWhiteSpace(next))
                {
                    sentenceEndTimes.Add(t);
                    sentenceEndCenters.Add(RevealedCenter(i));
                    t += sentenceEndPause;
                }
            }
        }
    }

    // Center of the text shown so far, used as the line punch pivot.
    Vector3 RevealedCenter(int last)
    {
        var info = text.textInfo;
        Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
        for (int i = 0; i <= last; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            min = Vector3.Min(min, ch.bottomLeft);
            max = Vector3.Max(max, ch.topRight);
        }
        return (min + max) * 0.5f;
    }

    void LateUpdate()
    {
        if (page < pages.Length - 1 && Time.time - startTime >= lineDuration + pageHold) ShowPage(page + 1);
        if (revealTimes.Length > 0) Apply();
    }

    void Apply()
    {
        float elapsed = Time.time - startTime;
        int sentenceEnd = LastSentenceEnd(elapsed);
        float since = sentenceEnd >= 0 ? elapsed - sentenceEndTimes[sentenceEnd] : float.MaxValue;
        Vector3 lineCenter = sentenceEnd >= 0 ? sentenceEndCenters[sentenceEnd] : Vector3.zero;

        float lift = Evaluate(bounceCurve, since, bounceDuration) * bounceHeight;
        float scale = (1f + Evaluate(punchCurve, since, punchDuration) * punchAmount) * (1f + Highlight * highlightScale);
        float shake = Evaluate(shakeFalloff, since, shakeDuration) * shakeStrength;
        int shakeStep = shake > 0f ? Mathf.FloorToInt(since * shakeFrequency) : 0;
        Vector3 lineShake = shake > 0f ? RandomOffset(shakeStep, 0) * shake : Vector3.zero;
        int jitterStep = Mathf.FloorToInt(Time.time * jitterFrequency);
        float jitter = jitterStrength * (1f + Highlight * highlightJitter);
        Vector3 lineJitter = RandomOffset(jitterStep, -1) * jitter;

        // Vertex colors are not converted by the TMP shaders in a linear project.
        bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color normal = linear ? textColor.linear : textColor;
        Color emphasized = linear ? emphasisColor.linear : emphasisColor;

        var info = text.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible) continue;

            float shown = elapsed - revealTimes[i];
            float alpha = letterFadeDuration > 0f ? Mathf.SmoothStep(0f, 1f, shown / letterFadeDuration) : shown >= 0f ? 1f : 0f;
            Color color = ch.index < emphasis.Count && emphasis[ch.index] ? emphasized : normal;
            color.a *= alpha;

            Vector3 pivot = punchPivot == PunchPivot.EachLetter ? (ch.bottomLeft + ch.topRight) * 0.5f : lineCenter;
            Vector3 offset = Vector3.up * lift;
            if (shake > 0f) offset += shakeLettersSeparately ? RandomOffset(shakeStep, i + 1) * shake : lineShake;
            if (jitter > 0f) offset += jitterLettersSeparately ? RandomOffset(jitterStep, -(i + 2)) * jitter : lineJitter;

            Write(text, textSource, i, pivot, scale, offset, color);
            if (glowLayer != null) Write(glowLayer, glowSource, i, pivot, scale, offset, color);
        }
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        if (glowLayer != null) glowLayer.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    // Index of the latest sentence end already reached, or -1.
    int LastSentenceEnd(float elapsed)
    {
        for (int i = sentenceEndTimes.Count - 1; i >= 0; i--)
            if (sentenceEndTimes[i] <= elapsed) return i;
        return -1;
    }

    static float Evaluate(AnimationCurve curve, float since, float duration) =>
        duration > 0f && since < duration ? curve.Evaluate(since / duration) : 0f;

    // Stable pseudo-random direction for a given time step and letter.
    static Vector3 RandomOffset(int step, int letter)
    {
        float Hash(float n) => Mathf.Repeat(Mathf.Sin(n) * 43758.5453f, 1f) * 2f - 1f;
        float seed = step * 12.9898f + letter * 78.233f;
        return new Vector3(Hash(seed), Hash(seed + 37.719f), 0f);
    }

    static void Write(TMP_Text target, TMP_MeshInfo[] source, int index, Vector3 pivot, float scale, Vector3 offset, Color32 color)
    {
        var ch = target.textInfo.characterInfo[index];
        int m = ch.materialReferenceIndex, v = ch.vertexIndex;
        var src = source[m].vertices;
        var dst = target.textInfo.meshInfo[m].vertices;
        var colors = target.textInfo.meshInfo[m].colors32;
        for (int j = 0; j < 4; j++)
        {
            dst[v + j] = pivot + (src[v + j] - pivot) * scale + offset;
            colors[v + j] = color;
        }
    }
}
