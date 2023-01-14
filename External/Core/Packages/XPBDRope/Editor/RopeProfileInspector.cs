using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NBG.XPBDRope
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RopeProfile), true, isFallback = true)]
    public class RopeProfileInspector : Editor
    {
        SerializedProperty profileData;
        SerializedProperty radius;
        SerializedProperty extraRendererRadius;
        SerializedProperty segmentLength;
        SerializedProperty useTwistLimits;
        SerializedProperty twistLimit;
        SerializedProperty elasticCompliance;
        SerializedProperty bendCompliance;
        SerializedProperty bendLimit;
        SerializedProperty massPerMeter;
        SerializedProperty drag;
        SerializedProperty angularDrag;
        SerializedProperty interpolation;
        SerializedProperty collisionDetectionMode;
        SerializedProperty physicMaterial;
        SerializedProperty linearSpring;
        SerializedProperty linearDamper;
        SerializedProperty maxSegmentSeparation;
        SerializedProperty slerpSpring;
        SerializedProperty slerpDamper;
        SerializedProperty angularMotion;

        bool jointFoldout = true;
        bool rigidbodyFoldout = true;
        bool rendererFoldout = true;

        private void OnEnable()
        {
            profileData = serializedObject.FindProperty("profileData");
            radius = FindProfileProperty("radius");
            extraRendererRadius = FindProfileProperty("extraRendererRadius");
            segmentLength = FindProfileProperty("segmentLength");
            useTwistLimits = FindProfileProperty("useTwistLimits");
            twistLimit = FindProfileProperty("twistLimit");
            elasticCompliance = FindProfileProperty("elasticCompliance");
            bendCompliance = FindProfileProperty("bendCompliance");
            bendLimit = FindProfileProperty("bendLimit");
            massPerMeter = FindProfileProperty("massPerMeter");
            drag = FindProfileProperty("drag");
            angularDrag = FindProfileProperty("angularDrag");
            interpolation = FindProfileProperty("interpolation");
            collisionDetectionMode = FindProfileProperty("collisionDetectionMode");
            physicMaterial = FindProfileProperty("physicMaterial");
            linearSpring = FindProfileProperty("linearSpring");
            linearDamper = FindProfileProperty("linearDamper");
            maxSegmentSeparation = FindProfileProperty("maxSegmentSeparation");
            slerpSpring = FindProfileProperty("slerpSpring");
            slerpDamper = FindProfileProperty("slerpDampingScale");
            angularMotion = FindProfileProperty("angularMotion");
        }

        SerializedProperty FindProfileProperty(string name)
        {
            return profileData.FindPropertyRelative(name);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(radius);
            EditorGUILayout.PropertyField(segmentLength);
            EditorGUILayout.PropertyField(useTwistLimits);
            if (useTwistLimits.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(twistLimit);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Rope elasticity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(elasticCompliance);
            EditorGUILayout.PropertyField(bendCompliance);
            EditorGUILayout.PropertyField(maxSegmentSeparation);
            EditorGUILayout.PropertyField(bendLimit);

            rigidbodyFoldout = EditorGUILayout.Foldout(rigidbodyFoldout, "Segment rigidbody settings");
            if (rigidbodyFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(massPerMeter);
                EditorGUILayout.PropertyField(drag);
                EditorGUILayout.PropertyField(angularDrag);
                EditorGUILayout.PropertyField(interpolation);
                EditorGUILayout.PropertyField(collisionDetectionMode);
                EditorGUILayout.PropertyField(physicMaterial);
                EditorGUI.indentLevel--;
            }

            jointFoldout = EditorGUILayout.Foldout(jointFoldout, "Segment joint settings");
            if (jointFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(linearSpring);
                EditorGUILayout.PropertyField(linearDamper);
                EditorGUILayout.PropertyField(angularMotion);
                EditorGUILayout.PropertyField(slerpSpring);
                EditorGUILayout.PropertyField(slerpDamper);
                EditorGUI.indentLevel--;
            }

            rendererFoldout = EditorGUILayout.Foldout(rendererFoldout, "Renderer");
            if (rendererFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(extraRendererRadius);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}