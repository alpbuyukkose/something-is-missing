using System.Collections;
using UnityEngine;

public class IngredientView : MonoBehaviour
{
    // How an ingredient flies between its shelf spot and the plate.
    [System.Serializable]
    public class MoveStyle
    {
        public float duration = 0.35f;
        [Tooltip("Height of the arc, world units.")]
        public float arcHeight = 0.2f;
        [Tooltip("Random +- share of the arc height per flight. 0.3 = 30% higher or lower.")]
        [Range(0f, 1f)] public float arcVariation = 0.3f;
        [Tooltip("Full turns around its own vertical axis during the flight.")]
        public float spinTurns = 1f;
        [Tooltip("Squash on landing: 0.15 = 15% flatter at the deepest point. 0 = off.")]
        public float landingSquash = 0.15f;
        public float landingDuration = 0.18f;
    }

    public IngredientData Data { get; private set; }
    public int PlateSlot { get; private set; } = -1;
    // Sits at the visual's center; hover effects move and rotate it, the root stays put.
    public Transform Pivot { get; private set; }
    public Vector3 RestCenter { get; private set; }

    Vector3 homePosition;
    Quaternion homeRotation;
    Vector3 seatOffset; // root offset that puts the visual's bottom center on a surface point (at scale 1)
    Collider[] colliders;

    // Call while this object still sits at the world origin, unrotated (bounds are computed in world space).
    public void Init(IngredientData data, GameObject greyboxPrefab, int layer)
    {
        Data = data;
        name = data.name;
        Pivot = new GameObject("Pivot").transform;
        Pivot.SetParent(transform, false);
        var visual = IngredientVisual.Create(data, greyboxPrefab, Pivot);

        // One fixed collider on the root, so a lifted or spinning visual never slips out from under the cursor.
        foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Destroy(c);
        var bounds = VisualBounds(visual);
        RestCenter = bounds.center;
        seatOffset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        Pivot.localPosition = RestCenter;
        visual.transform.localPosition -= RestCenter;
        var box = gameObject.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
        colliders = new Collider[] { box };

        foreach (var child in GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
    }

    public void SetHome(Vector3 position, Quaternion rotation)
    {
        homePosition = position;
        homeRotation = rotation;
        transform.SetPositionAndRotation(position, rotation);
    }

    // Sets the visual's bottom center on surfacePoint, scaled down or up to scale.
    public void MoveToPlate(int slot, Vector3 surfacePoint, Quaternion rotation, float scale, MoveStyle style)
    {
        PlateSlot = slot;
        Play(Move(surfacePoint + rotation * (seatOffset * scale), rotation, scale, style));
    }

    public void ReturnHome(MoveStyle style)
    {
        PlateSlot = -1;
        Play(Move(homePosition, homeRotation, 1f, style));
    }

    public void Consume(float duration)
    {
        SetClickable(false);
        Play(ScaleTo(0f, duration));
    }

    public void Respawn(float duration)
    {
        PlateSlot = -1;
        SetClickable(true);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        transform.localScale = Vector3.zero;
        Play(ScaleTo(1f, duration));
    }

    public void ResetHome()
    {
        StopAllCoroutines();
        PlateSlot = -1;
        SetClickable(true);
        transform.SetPositionAndRotation(homePosition, homeRotation);
        transform.localScale = Vector3.one;
    }

    static Bounds VisualBounds(GameObject visual)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 0.1f);
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    void SetClickable(bool value)
    {
        foreach (var c in colliders) c.enabled = value;
    }

    void Play(IEnumerator routine)
    {
        StopAllCoroutines();
        StartCoroutine(routine);
    }

    // Arcs to the target while turning around the visual's center, then squashes a little on landing.
    IEnumerator Move(Vector3 to, Quaternion toRotation, float toScale, MoveStyle style)
    {
        Vector3 from = transform.position;
        Quaternion fromRotation = transform.rotation;
        Vector3 fromScale = transform.localScale, targetScale = Vector3.one * toScale;
        float arc = style.arcHeight * (1f + Random.Range(-style.arcVariation, style.arcVariation));
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(style.duration, 0.0001f))
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            Vector3 position = Vector3.Lerp(from, to, e) + Vector3.up * (arc * Mathf.Sin(t * Mathf.PI));
            Quaternion rotation = Quaternion.Slerp(fromRotation, toRotation, e);
            transform.localScale = Vector3.Lerp(fromScale, targetScale, e);

            // Spin around the vertical line through the visual's center, so it does not orbit its pivot.
            var spin = Quaternion.AngleAxis(360f * style.spinTurns * e, Vector3.up);
            Vector3 center = position + rotation * Vector3.Scale(RestCenter, transform.localScale);
            transform.SetPositionAndRotation(center + spin * (position - center), spin * rotation);
            yield return null;
        }
        transform.SetPositionAndRotation(to, toRotation);
        transform.localScale = targetScale;

        if (style.landingSquash <= 0f) yield break;
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(style.landingDuration, 0.0001f))
        {
            float k = Mathf.Sin(t * Mathf.PI) * style.landingSquash;
            transform.localScale = Vector3.Scale(targetScale, new Vector3(1f + k * 0.5f, 1f - k, 1f + k * 0.5f));
            yield return null;
        }
        transform.localScale = targetScale;
    }

    IEnumerator ScaleTo(float target, float duration)
    {
        Vector3 from = transform.localScale, to = Vector3.one * target;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.localScale = to;
    }
}
