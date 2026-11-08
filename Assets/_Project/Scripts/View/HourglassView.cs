using System;
using UnityEngine;

// Shows the run timer: the sand runs from the top chamber into the bottom one over the whole run,
// and the hourglass gets more restless as time runs out. It never turns over.
public class HourglassView : MonoBehaviour
{
    [Serializable]
    public class Stage
    {
        [Tooltip("Remaining time where this stage is fully reached. 0.8 = 80% left.")]
        [Range(0f, 1f)] public float remaining = 1f;
        [Tooltip("Height above the table, world units.")]
        public float lift;
        [Tooltip("Tremble rotation, degrees.")]
        public float angle;
        [Tooltip("Tremble offset, world units.")]
        public float offset;
        [Tooltip("New tremble positions per second.")]
        public float frequency = 10f;
    }

    [SerializeField] RunTimer timer;
    [Tooltip("Lifted and trembled. The sand renderers must be its children.")]
    [SerializeField] Transform body;
    [Tooltip("Optional (both or none). Chamber-shaped sand using the Hourglass Sand shader; it fills from its bottom up along Body's up axis.")]
    [SerializeField] Renderer topSand;
    [SerializeField] Renderer bottomSand;

    [Header("Falling Sand")]
    [Tooltip("Optional. Grains falling from the neck; each lives just long enough to reach the bottom sand. Flows only while the timer runs.")]
    [SerializeField] ParticleSystem sandStream;

    [Header("Tension")]
    [Tooltip("From most time left to least. Values blend between neighbouring stages.")]
    [SerializeField] Stage[] stages =
    {
        new() { remaining = 1f },
        new() { remaining = 0.8f, angle = 0.6f, offset = 0.001f, frequency = 8f },
        new() { remaining = 0.55f, angle = 1f, offset = 0.0015f, frequency = 12f },
        new() { remaining = 0.5f, lift = 0.03f, angle = 1.5f, offset = 0.002f, frequency = 15f },
        new() { remaining = 0.2f, lift = 0.05f, angle = 4f, offset = 0.005f, frequency = 30f },
        new() { remaining = 0f, lift = 0.06f, angle = 7f, offset = 0.008f, frequency = 45f },
    };

    static readonly int SandPlane = Shader.PropertyToID("_SandPlane");
    MaterialPropertyBlock block;
    Vector2 topRange, bottomRange; // sand bottom/top heights in Body space
    Vector3 bodyPosition;
    Quaternion bodyRotation;
    float phase, seed;
    readonly Stage blended = new();

    bool HasSand => topSand != null && bottomSand != null;

    void Awake()
    {
        bodyPosition = body.localPosition;
        bodyRotation = body.localRotation;
        seed = UnityEngine.Random.Range(0f, 100f);
        if (!HasSand) return;
        block = new MaterialPropertyBlock();
        topRange = HeightRange(topSand);
        bottomRange = HeightRange(bottomSand);
    }

    void Update()
    {
        float remaining = timer.RemainingNormalized;
        Tremble(Evaluate(remaining));
        if (!HasSand) return;
        SetSand(remaining, 1f - remaining);
        if (sandStream != null) UpdateStream(timer.Running && remaining > 0.002f, 1f - remaining);
    }

    void UpdateStream(bool flowing, float bottomLevel)
    {
        var emission = sandStream.emission;
        emission.enabled = flowing;
        if (!flowing) return;
        // Fall time from the neck to the bottom sand's surface: d = v0 t + g t^2 / 2.
        var main = sandStream.main;
        float surface = body.TransformPoint(0f, Mathf.Lerp(bottomRange.x, bottomRange.y, bottomLevel), 0f).y;
        float d = Mathf.Max(0f, sandStream.transform.position.y - surface);
        float v0 = main.startSpeed.constant;
        float g = Physics.gravity.magnitude * main.gravityModifier.constant;
        main.startLifetime = Mathf.Max(0.01f, (Mathf.Sqrt(v0 * v0 + 2f * g * d) - v0) / g);
    }

    void Tremble(Stage s)
    {
        phase += Time.deltaTime * s.frequency; // accumulated so a changing frequency does not jump
        Vector3 offset = Vector3.up * s.lift + Noise(seed, phase) * s.offset;
        body.SetLocalPositionAndRotation(
            bodyPosition + body.parent.InverseTransformVector(offset),
            bodyRotation * Quaternion.Euler(Noise(seed + 50f, phase) * s.angle));
    }

    // Blends the two stages around this remaining time.
    Stage Evaluate(float remaining)
    {
        if (stages.Length == 0) return blended;
        for (int i = 1; i < stages.Length; i++)
        {
            Stage a = stages[i - 1], b = stages[i];
            if (remaining < b.remaining) continue;
            float t = Mathf.InverseLerp(a.remaining, b.remaining, remaining);
            blended.lift = Mathf.Lerp(a.lift, b.lift, t);
            blended.angle = Mathf.Lerp(a.angle, b.angle, t);
            blended.offset = Mathf.Lerp(a.offset, b.offset, t);
            blended.frequency = Mathf.Lerp(a.frequency, b.frequency, t);
            return blended;
        }
        return stages[^1];
    }

    // Smooth noise in -1..1 per axis.
    static Vector3 Noise(float seed, float t) => new(
        Mathf.PerlinNoise(seed, t) * 2f - 1f,
        Mathf.PerlinNoise(seed + 17.3f, t) * 2f - 1f,
        Mathf.PerlinNoise(seed + 31.7f, t) * 2f - 1f);

    Vector2 HeightRange(Renderer sand)
    {
        var b = sand.bounds;
        float a = body.InverseTransformPoint(b.min).y, c = body.InverseTransformPoint(b.max).y;
        return new Vector2(Mathf.Min(a, c), Mathf.Max(a, c));
    }

    void SetSand(float top, float bottom)
    {
        SetLevel(topSand, topRange, top);
        SetLevel(bottomSand, bottomRange, bottom);
    }

    // Cuts the sand with a plane that moves with Body, so the tremble tilts the sand with the glass.
    void SetLevel(Renderer sand, Vector2 range, float level)
    {
        sand.enabled = level > 0.001f;
        if (!sand.enabled) return;
        Vector3 normal = body.up;
        Vector3 point = body.TransformPoint(0f, Mathf.Lerp(range.x, range.y, level), 0f);
        sand.GetPropertyBlock(block);
        block.SetVector(SandPlane, new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point)));
        sand.SetPropertyBlock(block);
    }
}
