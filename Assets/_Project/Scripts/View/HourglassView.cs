using System.Collections;
using UnityEngine;

public class HourglassView : MonoBehaviour
{
    [SerializeField] GameController game;
    [Tooltip("Turned 180° around local Z on reset. The sand objects must be its children.")]
    [SerializeField] Transform body;
    [Tooltip("Scaled on local Y, 1 = full chamber. Pivot at the bottom of its chamber.")]
    [SerializeField] Transform topSand;
    [SerializeField] Transform bottomSand;
    [SerializeField] float flipDuration = 0.6f;

    Vector3 topFull, bottomFull;
    bool started, flipping;

    void Awake()
    {
        topFull = topSand.localScale;
        bottomFull = bottomSand.localScale;
    }

    void OnEnable() => game.DishStarted += OnDishStarted;
    void OnDisable() => game.DishStarted -= OnDishStarted;

    void OnDishStarted(int index, int length)
    {
        if (started)
        {
            StopAllCoroutines();
            StartCoroutine(Flip());
        }
        started = true;
    }

    void Update()
    {
        if (!flipping) SetSand(game.TimeNormalized, 1f - game.TimeNormalized);
    }

    void SetSand(float top, float bottom)
    {
        SetLevel(topSand, topFull, top);
        SetLevel(bottomSand, bottomFull, bottom);
    }

    static void SetLevel(Transform sand, Vector3 full, float level)
    {
        sand.gameObject.SetActive(level > 0.001f);
        sand.localScale = new Vector3(full.x, full.y * level, full.z);
    }

    IEnumerator Flip()
    {
        flipping = true;
        Quaternion start = body.localRotation;
        float top = topSand.gameObject.activeSelf ? topSand.localScale.y / topFull.y : 0f;
        float bottom = bottomSand.gameObject.activeSelf ? bottomSand.localScale.y / bottomFull.y : 0f;
        for (float t = 0f; t < 1f; t += Time.deltaTime / flipDuration)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            body.localRotation = start * Quaternion.Euler(0f, 0f, 180f * e);
            // The upper chamber empties and ends at the bottom, the lower one fills and ends on top.
            SetSand(Mathf.Lerp(top, 0f, e), Mathf.Lerp(bottom, 1f, e));
            yield return null;
        }
        body.localRotation = start; // symmetric shape: snapping back after 180° is invisible
        flipping = false;
    }
}
