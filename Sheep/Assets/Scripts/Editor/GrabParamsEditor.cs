using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

[CustomEditor(typeof(GrabParams))]

public class GrabParamsEditor : Editor
{
    UQueryBuilder<VisualElement> properties;
    UQueryBuilder<VisualElement> angularProperties;
    //https://forum.unity.com/threads/uielements-binding.571384/
    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/GrabParamsEditor.uss");
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/GrabParamsEditor.uxml");
        uxml.CloneTree(root);
        root.styleSheets.Add(uss);
        root.Bind(serializedObject);

        properties = root.Query<VisualElement>(className:"grabparamsrow");
        angularProperties = root.Query<VisualElement>(className: "angular");
        var angularControlToggle = root.Query<Toggle>("angularControl");
        
        var scheduledAction = root.schedule.Execute(() =>
        {
            var enableAngular = angularControlToggle.First().value;
            //Debug.Log(properties.First()[1]);
            properties.ForEach(f =>
            {
                if (f[1].childCount > 0)
                {
                    f[1][0][1].visible = (f[0] as Toggle).value;
                }
            });
            angularProperties.ForEach(f => f.style.display= enableAngular ? DisplayStyle.Flex : DisplayStyle.None);
        }
        );
        scheduledAction.Every(100);

        return root;
    }
}