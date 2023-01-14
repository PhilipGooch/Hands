using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlickQuadSwitchActivator))]
public class FlickQuadSwitchInspector : FlickSwitchBaseInspector
{
    protected override void ShowAdditionalContent()
    {
        AddProperty(serializedObject, "flickSwitchStartDirection");
    }


    protected override void ShowPositionCount()
    {
        var snapPositionCount = serializedObject.FindProperty("snapPositionCount");
        EditorGUI.BeginChangeCheck();
        var adjustedMinSnapPositions = minSnapPositions % 2 == 0 ? minSnapPositions + 1 : minSnapPositions;
        newSnapPositionsCount = (int)EditorGUILayout.Slider("Snap Positions Count", snapPositionCount.intValue, adjustedMinSnapPositions, maxSnapPositions);
        if (EditorGUI.EndChangeCheck())
        {
            //prevent even numbers for quad direction switch
            if (newSnapPositionsCount % 2 == 0)
            {
                newSnapPositionsCount = Mathf.Clamp(--newSnapPositionsCount, adjustedMinSnapPositions, maxSnapPositions);
            }
            snapPositionCount.intValue = newSnapPositionsCount;
        }
    }
}

