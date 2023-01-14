using NBG.Wind;
using UnityEditor;

[CustomEditor(typeof(Wind), false)]
[CanEditMultipleObjects]
public class WindInspector : WindZoneInspector
{
    public override void OnInspectorGUI()
    {
        DrawDefaultEditor();
    }

    protected override void DrawDefaultEditor()
    {
        base.DrawDefaultEditor();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("blockerLayers"));
        serializedObject.ApplyModifiedProperties();
    }
}
