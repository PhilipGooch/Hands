using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBG.VehicleSystem.Editor
{
    [CustomPropertyDrawer(typeof(PhysicalAxleSettings))]
    public class PhysicalAxleSettingsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            height += EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                height += GetPropertyHeight(property, "Guide");
                var guideProp = property.FindPropertyRelative("Guide");
                if (guideProp.objectReferenceValue == null)
                {
                    height += GetPropertyHeight(property, "ForwardOffset");
                    height += GetPropertyHeight(property, "VerticalOffset");
                    height += GetPropertyHeight(property, "HalfWidth");
                }
                height += GetPropertyHeight(property, "HubsWithWheels");
                height += GetPropertyHeight(property, "AdditionalMassForHubs");
                height += GetPropertyHeight(property, "IsSteerable");
                height += GetPropertyHeight(property, "SuspensionSettings");
                height += GetPropertyHeight(property, "MaxBrakeTorqueNm");
                height += GetPropertyHeight(property, "Differential");
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect rectFoldout = new Rect(position.min.x, position.min.y, position.size.x, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(rectFoldout, property.isExpanded, label);
            float yPosOffset = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                DrawSinglePropertyChild(position, property, "Guide", ref yPosOffset);
                var guideProp = property.FindPropertyRelative("Guide");
                if (guideProp.objectReferenceValue == null)
                {
                    DrawSinglePropertyChild(position, property, "ForwardOffset", ref yPosOffset);
                    DrawSinglePropertyChild(position, property, "VerticalOffset", ref yPosOffset);
                    DrawSinglePropertyChild(position, property, "HalfWidth", ref yPosOffset);
                }
                DrawSinglePropertyChild(position, property, "HubsWithWheels", ref yPosOffset);
                DrawSinglePropertyChild(position, property, "AdditionalMassForHubs", ref yPosOffset);
                DrawSinglePropertyChild(position, property, "IsSteerable", ref yPosOffset);
                DrawSinglePropertyChild(position, property, "SuspensionSettings", ref yPosOffset);
                DrawSinglePropertyChild(position, property, "MaxBrakeTorqueNm", ref yPosOffset);
                DrawSinglePropertyChild(position, property, "Differential", ref yPosOffset);
            }

            EditorGUI.EndProperty();
        }

        float GetPropertyHeight(SerializedProperty property, string name)
        {
            var childProp = property.FindPropertyRelative(name);
            return EditorGUI.GetPropertyHeight(childProp) + EditorGUIUtility.standardVerticalSpacing;
        }

        void DrawSinglePropertyChild(Rect position, SerializedProperty property, string name, ref float yPosOffset)
        {
            var childProp = property.FindPropertyRelative(name);
            var child_prop_height = EditorGUI.GetPropertyHeight(childProp);
            var rectType = new Rect(position.min.x, position.min.y + yPosOffset, position.size.x, child_prop_height);
            EditorGUI.PropertyField(rectType, childProp);
            yPosOffset += child_prop_height + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
