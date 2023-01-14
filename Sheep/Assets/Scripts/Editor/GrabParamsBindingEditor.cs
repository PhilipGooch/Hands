using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;

[CustomEditor(typeof(GrabParamsBinding))]
[CanEditMultipleObjects]

public class GrabParamsBindingEditor : Editor
{

    SerializedProperty grabParamsProperty;
    SerializedProperty priorityProperty;

    void OnEnable()
    {
        grabParamsProperty = serializedObject.FindProperty("grabParams");
        priorityProperty = serializedObject.FindProperty("priority");
    }

    VisualElement root;
    InspectorElement inspector;
    public override VisualElement CreateInspectorGUI()
    {
        root = new VisualElement();
        var grabParams = new PropertyField(grabParamsProperty);
        grabParams.RegisterCallback<ChangeEvent<UnityEngine.Object>>(ChangeGrabParams);
        root.Add(grabParams);
        root.Bind(serializedObject);
        BindGrabParamInspector();
        var priority = new PropertyField(priorityProperty);
        root.Add(priority);
        return root;
    }

    private void BindGrabParamInspector()
    {
        if (grabParamsProperty.hasMultipleDifferentValues || grabParamsProperty.objectReferenceValue == null)
        {
            if (inspector != null)
            {
                root.Remove(inspector);
                inspector = null;
            }
        }
        else
        {
            if (inspector == null)
            {
                inspector = new InspectorElement(grabParamsProperty.objectReferenceValue);
                root.Add(inspector);
            }
            else
                inspector.Bind(new SerializedObject(grabParamsProperty.objectReferenceValue));
        }
    }

    ////https://answers.unity.com/questions/1661534/unity-custom-inspector-createinspectorgui-redraw-o.html

    private void ChangeGrabParams(ChangeEvent<UnityEngine.Object> evt)
    {
        BindGrabParamInspector();

    }

}
