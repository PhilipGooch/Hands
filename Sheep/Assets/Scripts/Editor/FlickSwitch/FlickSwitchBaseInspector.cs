using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlickSwitchBase))]
public class FlickSwitchBaseInspector : Editor
{
    protected const int maxSnapPositions = 7;
    protected const int minSnapPositions = 2;

    protected int newSnapPositionsCount;

    protected virtual void OnEnable()
    {
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ShowAdditionalContent();

        EditorGUI.BeginChangeCheck();

        ShowPositionCount();

        var startSnapPosition = serializedObject.FindProperty("startSnapPosition");
        EditorGUI.BeginChangeCheck();
        var newStartSnapPosition = (int)EditorGUILayout.Slider("Start Snap Position", startSnapPosition.intValue, 0, newSnapPositionsCount - 1);
        if (EditorGUI.EndChangeCheck())
        {
            startSnapPosition.intValue = newStartSnapPosition;
        }

        AddProperty(serializedObject, "rotationArcDegrees");
        AddProperty(serializedObject, "switchLength");
        AddProperty(serializedObject, "switchAnimationDuration");
        AddProperty(serializedObject, "invertValue");

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void ShowAdditionalContent()
    {
    }

    protected virtual void ShowPositionCount()
    {
        EditorGUI.BeginChangeCheck();
        var snapPositionCount = serializedObject.FindProperty("snapPositionCount");
        newSnapPositionsCount = (int)EditorGUILayout.Slider("Snap Positions Count", snapPositionCount.intValue, minSnapPositions, maxSnapPositions);
        if (EditorGUI.EndChangeCheck())
        {
            snapPositionCount.intValue = newSnapPositionsCount;
        }
    }

    protected SerializedProperty AddProperty(SerializedObject baseProperty, string propertyName, string displayName = "")
    {
        var property = baseProperty.FindProperty(propertyName);

        if (string.IsNullOrWhiteSpace(displayName))
            EditorGUILayout.PropertyField(property, true);
        else
            EditorGUILayout.PropertyField(property, new GUIContent(displayName), true);

        return property;
    }
}

