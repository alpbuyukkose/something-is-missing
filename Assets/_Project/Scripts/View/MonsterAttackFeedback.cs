using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Environment reactions to the dragon's bites: camera, props, light and particles.
// Call PlayPlateBite / PlayPlayerBite at the moment of the bite (dragon events or animation events).
public class MonsterAttackFeedback : MonoBehaviour
{
    [Serializable]
    public class Shake
    {
        public bool enabled = true;
        [Tooltip("Seconds after the bite.")]
        public float delay;
        public float duration = 0.25f;
        [Tooltip("Max offset along the object's local axes, in world units.")]
        public Vector3 positionAmplitude;
        [Tooltip("Max rotation around the object's local axes, in degrees.")]
        public Vector3 rotationAmplitude;
        [Tooltip("Noise speed. Higher = sharper and more violent.")]
        public float frequency = 25f;
        [Tooltip("Strength over time (0-1).")]
        public AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }

    [Serializable]
    public class Swing
    {
        public bool enabled = true;
        [Tooltip("Seconds after the bite.")]
        public float delay;
        public float duration = 1f;
        [Tooltip("Peak angle in degrees.")]
        public float angle = 5f;
        [Tooltip("Local axis the object swings around.")]
        public Vector3 axis = Vector3.right;
        [Tooltip("Swings per second.")]
        public float frequency = 4f;
        [Tooltip("Small upward hop that follows the swing, in world units. 0 = none.")]
        public float hop;
        [Tooltip("Strength over time (0-1).")]
        public AnimationCurve falloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    }

    [Serializable]
    public class FovKick
    {
        public bool enabled = true;
        [Tooltip("Seconds after the bite.")]
        public float delay;
        public float duration = 0.3f;
        [Tooltip("Degrees added at the peak. Negative = slight zoom in.")]
        public float amount = -1.5f;
        [Tooltip("Amount over time (0-1). End at 0 to return to the original FOV.")]
        public AnimationCurve curve = new(new Keyframe(0f, 0f, 0f, 14f), new Keyframe(0.12f, 1f), new Keyframe(1f, 0f));
    }

    [Serializable]
    public class Burst
    {
        public bool enabled = true;
        [Tooltip("Seconds after the bite.")]
        public float delay;
        [Tooltip("Where the burst starts.")]
        public Transform origin;
        [Tooltip("On: the burst circle is turned to face the camera. Off: it takes the origin's rotation.")]
        public bool faceCamera = true;
    }

    [Serializable]
    public class Profile
    {
        public Shake cameraShake = new();
        public FovKick fovKick = new();
        public Burst burst = new();
        public Swing plateWobble = new();
        public Shake tableShake = new();
        public Shake hourglassShake = new();
        public Shake forkShake = new();
        public Swing lightSwing = new();
    }

    [Header("References")]
    [SerializeField] Camera cam;
    [SerializeField] Transform plate;
    [SerializeField] Transform table;
    [Tooltip("Shaken as a whole; the hourglass flip runs on its Body child.")]
    [SerializeField] Transform hourglass;
    [Tooltip("Its fork (created at runtime) is shaken with forkShake.")]
    [SerializeField] Pantry pantry;
    [SerializeField] Transform swingLight;
    [Tooltip("Look, count, size, speed and lifetime are edited on this particle system (BiteBurst prefab).")]
    [SerializeField] ParticleSystem burstParticles;

    [Header("Profiles")]
    [SerializeField] Profile plateBite = new();
    [SerializeField] Profile playerBite = new();

    struct Running
    {
        public Transform target;
        public Shake shake;
        public Swing swing;
        public float start;
        public float seed;
    }

    readonly List<Running> running = new();
    readonly Dictionary<Transform, (Vector3 position, Quaternion rotation)> basePoses = new();
    readonly Dictionary<Transform, (Vector3 position, Quaternion rotation)> offsets = new();
    readonly List<Transform> finished = new();
    readonly List<(FovKick kick, float start)> fovKicks = new();
    float baseFov;

    public void PlayPlateBite() => Play(plateBite);
    public void PlayPlayerBite() => Play(playerBite);

    void Play(Profile p)
    {
        Add(cam ? cam.transform : null, p.cameraShake);
        Add(table, p.tableShake);
        Add(hourglass, p.hourglassShake);
        Add(pantry != null && pantry.Fork != null ? pantry.Fork.transform : null, p.forkShake);
        Add(plate, p.plateWobble);
        Add(swingLight, p.lightSwing);

        if (p.fovKick.enabled && cam != null)
        {
            if (fovKicks.Count == 0) baseFov = cam.fieldOfView;
            fovKicks.Add((p.fovKick, Time.time + p.fovKick.delay));
        }
        if (p.burst.enabled && burstParticles != null) StartCoroutine(EmitBurst(p.burst));
    }

    void Add(Transform target, Shake shake)
    {
        if (target != null && shake.enabled) Track(new Running { target = target, shake = shake, start = Time.time + shake.delay });
    }

    void Add(Transform target, Swing swing)
    {
        if (target != null && swing.enabled) Track(new Running { target = target, swing = swing, start = Time.time + swing.delay });
    }

    void Track(Running r)
    {
        if (!basePoses.ContainsKey(r.target)) basePoses[r.target] = (r.target.localPosition, r.target.localRotation);
        r.seed = UnityEngine.Random.Range(0f, 100f);
        running.Add(r);
    }

    void LateUpdate()
    {
        UpdateTransforms();
        UpdateFov();
    }

    void UpdateTransforms()
    {
        if (basePoses.Count == 0) return;
        offsets.Clear();
        foreach (var target in basePoses.Keys) offsets[target] = (Vector3.zero, Quaternion.identity);

        for (int i = running.Count - 1; i >= 0; i--)
        {
            var r = running[i];
            float t = Time.time - r.start;
            float duration = r.shake != null ? r.shake.duration : r.swing.duration;
            if (t >= duration) { running.RemoveAt(i); continue; }
            if (t < 0f) continue;

            var (pos, rot) = offsets[r.target];
            if (r.shake != null) AddShake(r, t, ref pos, ref rot);
            else AddSwing(r, t, ref pos, ref rot);
            offsets[r.target] = (pos, rot);
        }

        finished.Clear();
        foreach (var (target, pose) in basePoses)
        {
            if (target == null) { finished.Add(target); continue; }
            var (pos, rot) = offsets[target];
            target.SetLocalPositionAndRotation(pose.position + pose.rotation * pos, pose.rotation * rot);
            if (!IsRunning(target)) finished.Add(target);
        }
        foreach (var target in finished) basePoses.Remove(target);
    }

    bool IsRunning(Transform target)
    {
        foreach (var r in running)
            if (r.target == target) return true;
        return false;
    }

    static void AddShake(Running r, float t, ref Vector3 pos, ref Quaternion rot)
    {
        var s = r.shake;
        float k = s.falloff.Evaluate(t / s.duration);
        float n = t * s.frequency;
        pos += Vector3.Scale(Noise(r.seed, n), s.positionAmplitude) * k;
        rot *= Quaternion.Euler(Vector3.Scale(Noise(r.seed + 50f, n), s.rotationAmplitude) * k);
    }

    static void AddSwing(Running r, float t, ref Vector3 pos, ref Quaternion rot)
    {
        var s = r.swing;
        float k = s.falloff.Evaluate(t / s.duration);
        float wave = Mathf.Sin(t * s.frequency * 2f * Mathf.PI);
        rot *= Quaternion.AngleAxis(wave * s.angle * k, s.axis);
        pos += Vector3.up * (Mathf.Abs(wave) * s.hop * k);
    }

    // Smooth noise in -1..1 per axis.
    static Vector3 Noise(float seed, float t) => new(
        Mathf.PerlinNoise(seed, t) * 2f - 1f,
        Mathf.PerlinNoise(seed + 17.3f, t) * 2f - 1f,
        Mathf.PerlinNoise(seed + 31.7f, t) * 2f - 1f);

    void UpdateFov()
    {
        if (fovKicks.Count == 0) return;
        float offset = 0f;
        for (int i = fovKicks.Count - 1; i >= 0; i--)
        {
            var (kick, start) = fovKicks[i];
            float t = Time.time - start;
            if (t >= kick.duration) { fovKicks.RemoveAt(i); continue; }
            if (t >= 0f) offset += kick.curve.Evaluate(t / kick.duration) * kick.amount;
        }
        cam.fieldOfView = baseFov + offset;
    }

    IEnumerator EmitBurst(Burst b)
    {
        if (b.delay > 0f) yield return new WaitForSeconds(b.delay);
        Vector3 position = b.origin ? b.origin.position : transform.position;
        // The Circle shape spreads on the system's local XY plane, so facing the camera = camera rotation.
        Quaternion rotation = b.faceCamera && cam != null ? cam.transform.rotation
            : b.origin ? b.origin.rotation : Quaternion.identity;
        burstParticles.transform.SetPositionAndRotation(position, rotation);
        burstParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        burstParticles.Play(true);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        foreach (var (target, pose) in basePoses)
            if (target != null) target.SetLocalPositionAndRotation(pose.position, pose.rotation);
        basePoses.Clear();
        running.Clear();
        if (fovKicks.Count > 0 && cam != null) cam.fieldOfView = baseFov;
        fovKicks.Clear();
    }
}
