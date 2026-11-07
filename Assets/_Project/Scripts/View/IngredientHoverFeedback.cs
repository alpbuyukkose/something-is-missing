using System.Collections.Generic;
using UnityEngine;

// Hovered ingredient lifts, leans and spins slowly.
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

    class Pose
    {
        public float weight;
        public float spin;
    }

    readonly Dictionary<IngredientView, Pose> poses = new();
    readonly List<IngredientView> settled = new();
    IngredientView hovered;

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
    }

    void Apply(IngredientView view, Pose pose)
    {
        float e = Mathf.SmoothStep(0f, 1f, pose.weight);
        var target = Quaternion.Euler(tilt) * Quaternion.Euler(0f, pose.spin, 0f);
        view.Pivot.SetLocalPositionAndRotation(
            view.RestCenter + Vector3.up * (liftHeight * e),
            Quaternion.Slerp(Quaternion.identity, target, e));
    }

    void OnDisable()
    {
        foreach (var view in poses.Keys)
            if (view != null) view.Pivot.SetLocalPositionAndRotation(view.RestCenter, Quaternion.identity);
        poses.Clear();
    }
}
