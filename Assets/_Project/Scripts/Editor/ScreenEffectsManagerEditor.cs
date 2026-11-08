using UnityEditor;
using UnityEngine;

// Adds an on/off toggle for every screen effect on the same object.
[CustomEditor(typeof(ScreenEffectsManager))]
public class ScreenEffectsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        foreach (var effect in ((Component)target).GetComponents<ScreenEffect>())
        {
            bool on = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(effect.GetType().Name), effect.enabled);
            if (on == effect.enabled) continue;
            Undo.RecordObject(effect, "Toggle Screen Effect");
            effect.enabled = on;
        }
    }
}
