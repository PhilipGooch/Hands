using System;
using UnityEditor;
using UnityEngine;

namespace NBG.Audio.Editor
{ 
    [CustomPropertyDrawer(typeof(SurfaceType))]
    public class SurfaceTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var idProp = property.FindPropertyRelative("id");

            if (SurfaceTypes.TypeValues == null)
                SurfaceTypes.EnsureInitialized();

            var index = Array.IndexOf(SurfaceTypes.TypeValues, idProp.intValue);
            index = EditorGUI.Popup(position, label.text, index, SurfaceTypes.TypeNames);
            var value = SurfaceTypes.TypeValues[index];

            idProp.intValue = value;
            
            EditorGUI.EndProperty();
        }
    }

}
