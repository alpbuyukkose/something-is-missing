using UnityEditor;
using UnityEngine;

// Wherever a SoundCue is referenced (e.g. SoundManager's lists), its settings unfold right there.
// Edits change the cue asset itself, so every place that uses the cue hears them.
[CustomPropertyDrawer(typeof(SoundCue))]
public class SoundCueDrawer : PropertyDrawer
{
    static float Line => EditorGUIUtility.singleLineHeight;
    static float Gap => EditorGUIUtility.standardVerticalSpacing;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded || property.objectReferenceValue == null) return Line;
        float height = Line;
        var it = FirstField(new SerializedObject(property.objectReferenceValue));
        do height += EditorGUI.GetPropertyHeight(it, true) + Gap;
        while (it.NextVisible(false));
        return height + Gap;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        const float buttonWidth = 26f;
        var header = new Rect(position.x, position.y, position.width - buttonWidth - 2f, Line);
        if (property.objectReferenceValue != null)
        {
            property.isExpanded = EditorGUI.Foldout(new Rect(header.x, header.y, EditorGUIUtility.labelWidth, Line), property.isExpanded, GUIContent.none, true);
            if (GUI.Button(new Rect(header.xMax + 2f, header.y, buttonWidth, Line), "▶")) SoundCuePreview.Play((SoundCue)property.objectReferenceValue);
        }
        EditorGUI.PropertyField(header, property, label);
        if (!property.isExpanded || property.objectReferenceValue == null) return;

        var cue = new SerializedObject(property.objectReferenceValue);
        cue.Update();
        EditorGUI.indentLevel++;
        float y = header.yMax + Gap;
        var it = FirstField(cue);
        do
        {
            float height = EditorGUI.GetPropertyHeight(it, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), it, true);
            y += height + Gap;
        }
        while (it.NextVisible(false));
        EditorGUI.indentLevel--;
        cue.ApplyModifiedProperties();
    }

    // The cue's first field after the script reference.
    static SerializedProperty FirstField(SerializedObject cue)
    {
        var it = cue.GetIterator();
        it.NextVisible(true);  // m_Script
        it.NextVisible(false);
        return it;
    }
}
