using UnityEditor;
using UnityEngine;

namespace NBG.Wind
{
    [CustomEditor(typeof(WindZone), false)]
    [CanEditMultipleObjects]
    public class WindZoneInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultEditor();
        }

        protected virtual void DrawDefaultEditor()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("General Setup", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var mode = CreatePropertyField("mode");
            var volumeType = CreatePropertyField("volumeType");
            CreatePropertyField("affectedLayers");

            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("AirZone", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            CreatePropertyField("airZoneOffset");
            CreatePropertyField("airZoneLength");
            if ((WindZoneVolumeType)volumeType.enumValueIndex == WindZoneVolumeType.Cylinder)
                CreatePropertyField("airZoneRadius");
            else if ((WindZoneVolumeType)volumeType.enumValueIndex == WindZoneVolumeType.Box)
                CreatePropertyField("airZoneDimensions");


            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Forces", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var useOscillator = CreatePropertyField("oscillatingPower");
            if (useOscillator.boolValue)
            {
                CreatePropertyField("oscillatorFrequency");
                CreatePropertyField("forceMin");
                CreatePropertyField("forceMax");
            }
            else
                CreatePropertyField("forceMax", "Force");

            CreatePropertyField("forceFalloff");

            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        SerializedProperty CreatePropertyField(string name, string customName = "")
        {
            var obj = serializedObject.FindProperty(name);
            if (!string.IsNullOrEmpty(customName))
                EditorGUILayout.PropertyField(obj, new GUIContent() { text = customName });
            else
                EditorGUILayout.PropertyField(obj);

            return obj;
        }
    }
}
