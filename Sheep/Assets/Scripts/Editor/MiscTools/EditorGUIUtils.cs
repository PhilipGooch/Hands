using System;
using UnityEditor;
using UnityEngine;

public static class EditorGUIUtils
{
    public static void ButtonAndFloatField(string buttonText, Action<float> onButtonClick, ref float targetFloat)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(buttonText))
        {
            onButtonClick(targetFloat);
        }
        targetFloat = EditorGUILayout.FloatField(targetFloat);
        EditorGUILayout.EndHorizontal();
    }
}
