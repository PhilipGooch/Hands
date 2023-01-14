using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NBG.XPBDRope
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Rope), true, isFallback = true)]
    public class RopeInspector : Editor
    {
        SerializedProperty ropeProfile;
        SerializedProperty attachStartTo;
        SerializedProperty attachEndTo;
        SerializedProperty fixRopeStart;
        SerializedProperty fixRopeEnd;
        SerializedProperty fixRopeStartRotation;
        SerializedProperty fixRopeEndRotation;
        
        SerializedProperty ropeLengthMultiplier;
        SerializedProperty baseRopeLength;
        SerializedProperty extraRopeLength;
        SerializedProperty handles;
        SerializedProperty bones;
        SerializedProperty useProfileOverride;
        SerializedProperty profileOverride;

        private void OnEnable()
        {
            ropeProfile = serializedObject.FindProperty("ropeProfile");
            attachStartTo = serializedObject.FindProperty("attachStartTo");
            attachEndTo = serializedObject.FindProperty("attachEndTo");
            fixRopeStart = serializedObject.FindProperty("fixRopeStart");
            fixRopeEnd = serializedObject.FindProperty("fixRopeEnd");
            fixRopeStartRotation = serializedObject.FindProperty("fixRopeStartRotation");
            fixRopeEndRotation = serializedObject.FindProperty("fixRopeEndRotation");
            ropeLengthMultiplier = serializedObject.FindProperty("ropeLengthMultiplier");
            baseRopeLength = serializedObject.FindProperty("baseRopeLength");
            extraRopeLength = serializedObject.FindProperty("extraRopeLength");
            handles = serializedObject.FindProperty("handles");
            bones = serializedObject.FindProperty("bones");
            useProfileOverride = serializedObject.FindProperty("useProfileOverride");
            profileOverride = serializedObject.FindProperty("profileOverride");
        }

        public override void OnInspectorGUI()
        {
            var rope = serializedObject.targetObject as Rope;
            if (rope.IsBuilt)
            {
                if (rope.IsOutdated)
                {
                    EditorGUILayout.HelpBox($"This rope is outdated and needs to be rebuilt to work properly!\nExpected version {Rope.latestRopeVersion} but was {rope.Version}.", MessageType.Warning);
                }
                else
                {
                    var report = rope.RopeDeltaReport;
                    if (!string.IsNullOrEmpty(report))
                    {
                        EditorGUILayout.HelpBox($"This rope has some changes and needs to be rebuilt!\n{report}", MessageType.Warning);
                    }
                }
            }
            EditorGUILayout.PropertyField(ropeProfile);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Attachment Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(attachStartTo);
            EditorGUILayout.PropertyField(attachEndTo);
            EditorGUILayout.PropertyField(fixRopeStart);
            EditorGUILayout.PropertyField(fixRopeEnd);
            EditorGUILayout.PropertyField(fixRopeStartRotation);
            EditorGUILayout.PropertyField(fixRopeEndRotation);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Rope Length", EditorStyles.boldLabel);
            DrawLengthAndResetButton();
            EditorGUILayout.PropertyField(ropeLengthMultiplier);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(useProfileOverride);
            if (useProfileOverride.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(profileOverride);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(handles);
            EditorGUILayout.PropertyField(bones);

            //DrawDefaultInspector();
            DrawRopeCreationButtons();
            serializedObject.ApplyModifiedProperties();
        }

        protected void DrawRopeCreationButtons()
        {
            var targets = serializedObject.targetObjects;

            if (GUILayout.Button("Build Rope"))
            {
                foreach (Rope rope in targets)
                {
                    RopeBuilder.BuildRope(rope, OnSegmentCreated, OnObjectConnected);
                }
            }
            if (GUILayout.Button("Clear Rope"))
            {
                foreach (Rope rope in targets)
                {
                    rope.ClearRope();
                }
            }
        }

        protected void DrawLengthAndResetButton()
        {
            EditorGUILayout.BeginHorizontal();
            var targets = serializedObject.targetObjects;

            if (!Application.isPlaying)
            {
                foreach (Rope rope in targets)
                {
                    // Update rope length if the handles move, but only in edit mode.
                    rope.RecalculateBaseLength();
                }
            }

            float maxLength = EditorGUILayout.FloatField("Max Rope Length", baseRopeLength.floatValue + extraRopeLength.floatValue);
            extraRopeLength.floatValue = Mathf.Clamp(maxLength - baseRopeLength.floatValue, 0f, float.MaxValue);

            //EditorGUILayout.PropertyField(maxRopeLength);

            if (GUILayout.Button("Reset"))
            {
                foreach(Rope rope in targets)
                {
                    rope.ResetLength();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        protected virtual void OnSegmentCreated(RopeSegment segment)
        {

        }

        protected virtual void OnObjectConnected(Rigidbody connectedObject)
        {

        }
    }
}

