using System.Collections.Generic;
using UnityEngine;

// Hovered ingredient lifts, leans and spins slowly; one shared spot light shines on it.
public class IngredientHoverFeedback : MonoBehaviour
{
    [Header("Ingredient")]
    [Tooltip("How far it rises, in world units.")]
    [SerializeField] float liftHeight = 0.04f;
    [Tooltip("Fixed lean while hovered, degrees around the ingredient's own axes. -X leans toward the front, +Z leans left.")]
    [SerializeField] Vector3 tilt = new(-10f, 0f, 12f);
    [Tooltip("Spin around its own up axis while hovered, degrees per second.")]
    [SerializeField] float spinSpeed = 45f;
    [Tooltip("Seconds to ease into and out of the hover pose.")]
    [SerializeField] float transitionTime = 0.15f;

    [Header("Light")]
    [SerializeField] Light hoverLight;
    [Tooltip("Light position relative to the ingredient's resting pose: X right, Y up, -Z front.")]
    [SerializeField] Vector3 lightOffset = new(0f, 0.3f, -0.25f);
    [SerializeField] float lightIntensity = 2f;
    [Tooltip("Seconds for the light to fade in or out.")]
    [SerializeField] float lightFade = 0.12f;

    class Pose
    {
        public float weight;
        public float spin;
    }

    readonly Dictionary<IngredientView, Pose> poses = new();
    readonly List<IngredientView> settled = new();
    IngredientView hovered, lit;
    float lightWeight;

    public void SetHovered(IngredientView view) => hovered = view;

    void LateUpdate()
    {
        if (hovered != null && !poses.ContainsKey(hovered)) poses[hovered] = new Pose();

        settled.Clear();
        foreach (var (view, pose) in poses)
        {
            bool on = view == hovered;
            pose.weight = Mathf.MoveTowards(pose.weight, on ? 1f : 0f, Time.deltaTime / Mathf.Max(transitionTime, 0.0001f));
            if (on) pose.spin += spinSpeed * Time.deltaTime;
            Apply(view, pose);
            if (!on && pose.weight <= 0f) settled.Add(view);
        }
        foreach (var view in settled) poses.Remove(view);

        UpdateLight();
    }

    void Apply(IngredientView view, Pose pose)
    {
        float e = Mathf.SmoothStep(0f, 1f, pose.weight);
        var target = Quaternion.Euler(tilt) * Quaternion.Euler(0f, pose.spin, 0f);
        view.Pivot.SetLocalPositionAndRotation(
            view.RestCenter + Vector3.up * (liftHeight * e),
            Quaternion.Slerp(Quaternion.identity, target, e));
    }

    void UpdateLight()
    {
        if (hoverLight == null) return;
        if (hovered != null) lit = hovered;
        lightWeight = Mathf.MoveTowards(lightWeight, hovered != null ? 1f : 0f, Time.deltaTime / Mathf.Max(lightFade, 0.0001f));
        hoverLight.enabled = lightWeight > 0f && lit != null;
        if (!hoverLight.enabled) return;

        hoverLight.intensity = lightIntensity * Mathf.SmoothStep(0f, 1f, lightWeight);
        hoverLight.transform.position = lit.transform.TransformPoint(lit.RestCenter + lightOffset);
        hoverLight.transform.LookAt(lit.Pivot.position);
    }

    void OnDisable()
    {
        foreach (var view in poses.Keys)
            if (view != null) view.Pivot.SetLocalPositionAndRotation(view.RestCenter, Quaternion.identity);
        poses.Clear();
        if (hoverLight != null) hoverLight.enabled = false;
        lightWeight = 0f;
    }
}
