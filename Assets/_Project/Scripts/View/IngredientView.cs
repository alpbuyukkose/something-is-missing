using System.Collections;
using UnityEngine;

public class IngredientView : MonoBehaviour
{
    public IngredientData Data { get; private set; }
    public int PlateSlot { get; private set; } = -1;
    // Sits at the visual's center; hover effects move and rotate it, the root stays put.
    public Transform Pivot { get; private set; }
    public Vector3 RestCenter { get; private set; }

    Vector3 homePosition;
    Quaternion homeRotation;
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

    public void MoveToPlate(int slot, Vector3 position, Quaternion rotation, float duration, float arc)
    {
        PlateSlot = slot;
        Play(Move(position, rotation, duration, arc));
    }

    public void ReturnHome(float duration, float arc)
    {
        PlateSlot = -1;
        Play(Move(homePosition, homeRotation, duration, arc));
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

    IEnumerator Move(Vector3 to, Quaternion toRotation, float duration, float arc)
    {
        Vector3 from = transform.position;
        Quaternion fromRotation = transform.rotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            transform.SetPositionAndRotation(
                Vector3.Lerp(from, to, e) + Vector3.up * (arc * Mathf.Sin(t * Mathf.PI)),
                Quaternion.Slerp(fromRotation, toRotation, e));
            yield return null;
        }
        transform.SetPositionAndRotation(to, toRotation);
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
