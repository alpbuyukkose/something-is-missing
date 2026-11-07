using UnityEditor;
using UnityEngine;

// Plays a cue with its current settings from the Inspector, in or out of Play mode.
public static class SoundCuePreview
{
    static AudioSource source;

    public static void Play(SoundCue cue)
    {
        if (cue == null) return;
        if (source == null)
            source = EditorUtility.CreateGameObjectWithHideFlags("SoundCuePreview", HideFlags.HideAndDontSave, typeof(AudioSource)).GetComponent<AudioSource>();
        cue.PlayOn(source);
    }
}

// ▶ button on the cue asset's own Inspector.
[CustomEditor(typeof(SoundCue))]
public class SoundCueEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        if (GUILayout.Button("▶ Play")) SoundCuePreview.Play((SoundCue)target);
    }
}
