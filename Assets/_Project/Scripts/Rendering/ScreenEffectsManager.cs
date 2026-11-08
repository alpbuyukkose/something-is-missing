using UnityEngine;

// One place for the screen effects: a master switch and whether text goes through them.
// The effects themselves are the ScreenEffect components (Retro, Vignette, Film Grain, Scanline...).
[ExecuteAlways]
public class ScreenEffectsManager : MonoBehaviour
{
    [Tooltip("Off = no screen effect at all, to compare before and after.")]
    [SerializeField] bool effectsOn = true;
    [Tooltip("On: menus, the poem, the end screen and the dragon's lines go through the effects. Off: they are drawn clean on top.")]
    [SerializeField] bool affectText = true;

    [Header("Text")]
    [SerializeField] Camera gameCamera;
    [Tooltip("Screen menus. While Affect Text is on they render through the game camera.")]
    [SerializeField] Canvas[] canvases;
    [Tooltip("Texts in the 3D scene (e.g. the dialog). While Affect Text is off they move to the exempt layer.")]
    [SerializeField] GameObject[] worldTexts;
    [Tooltip("Layer the renderers draw after post-processing, right before the screen effects (so fog and tonemapping skip the menus).")]
    [SerializeField] string uiLayer = "ScreenFxUI";
    [Tooltip("Layer the renderers draw after the screen effects.")]
    [SerializeField] string exemptLayer = "ScreenFxExempt";

    bool applied, appliedAffectText;

    void OnEnable() => applied = false;

    void OnDisable() => ScreenEffect.AllEnabled = true;

    void Update()
    {
        ScreenEffect.AllEnabled = effectsOn;
        if (applied && appliedAffectText == affectText) return;
        applied = true;
        appliedAffectText = affectText;
        RouteText();
    }

    void RouteText()
    {
        int ui = LayerMask.NameToLayer(uiLayer);
        bool throughEffects = affectText && gameCamera != null && ui >= 0;
        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;
            if (throughEffects)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = gameCamera;
                canvas.planeDistance = gameCamera.nearClipPlane + 0.01f;
            }
            else canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            SetLayer(canvas.gameObject, throughEffects ? ui : LayerMask.NameToLayer("UI"));
        }

        int exempt = LayerMask.NameToLayer(exemptLayer);
        int layer = affectText || exempt < 0 ? 0 : exempt; // 0 = Default
        foreach (var root in worldTexts) SetLayer(root, layer);
    }

    static void SetLayer(GameObject root, int layer)
    {
        if (root == null) return;
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }
}
