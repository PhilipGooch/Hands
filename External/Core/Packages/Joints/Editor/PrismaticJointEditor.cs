using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NBG.Joints
{
    /// <summary>
    /// Custom inspector for prismatic Joint. It renders gizmos and hides unused/disabled parameters.
    /// </summary>
    [CustomEditor(typeof(PrismaticJoint)), CanEditMultipleObjects]
    class PrismaticJointEditor : Editor
    {
        private SerializedProperty attachmentMode, start, end, progress, attachedBody, profile, overrideProfile, springOverride, damperOverride;

        public enum EditMode
        {
            None,
            Start,
            End
        }

        private EditMode mode = EditMode.None;

        private const string projectConfigGUIDPrefKey = "NBG.Joints.ProjectConfigGUID";
        private const string projectConfigFileName = "ProjectJointProfilesConfig";
        private const string defaultConfigFileName = "JointProfilesConfig";

        private void OnEnable()
        {
            attachmentMode = serializedObject.FindProperty("attachmentMode");
            start = serializedObject.FindProperty("start");
            end = serializedObject.FindProperty("end");
            progress = serializedObject.FindProperty("progress");
            profile = serializedObject.FindProperty("profile");
            attachedBody = serializedObject.FindProperty("attachedBody");
            overrideProfile = serializedObject.FindProperty("overrideProfile");
            springOverride = serializedObject.FindProperty("springOverride");
            damperOverride = serializedObject.FindProperty("damperOverride");

            SetDefaultProjectProfiles();
        }

        private void OnDisable()
        {
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PrismaticJoint prismaticJoint = (PrismaticJoint)target;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(start);
            if (GUILayout.Button(mode != EditMode.Start ? "Edit" : "Stop edit"))
            {
                mode = mode != EditMode.Start ? EditMode.Start : EditMode.None;
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(end);
            if (GUILayout.Button(mode != EditMode.End ? "Edit" : "Stop edit"))
            {
                mode = mode != EditMode.End ? EditMode.End : EditMode.None;
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            string[] profileOptions;
            int profileIndex = 0;
            try
            {
                var profiles = JointsEditorSettings.GetOrCreateSettings().PrismaticJointProfiles;
                profileOptions = profiles.Select(e => e.ProfileName).ToArray();
                profileIndex = profiles.FindIndex(e => e == profile.objectReferenceValue);
            }
            catch
            {
                Debug.LogError("Project joint profiles file is empty");
                return;
            }

            EditorGUILayout.PropertyField(overrideProfile);

            if (overrideProfile.boolValue)
            {
                EditorGUILayout.PropertyField(springOverride);
                EditorGUILayout.PropertyField(damperOverride);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUI.BeginChangeCheck();
                    profileIndex = EditorGUILayout.Popup("Spring mode", profileIndex, profileOptions);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(profile, GUIContent.none, GUILayout.Width(64)); // Debug reference
                    EditorGUI.EndDisabledGroup();

                    if (EditorGUI.EndChangeCheck())
                    {
                        var profiles = JointsEditorSettings.GetOrCreateSettings().PrismaticJointProfiles;
                        profile.objectReferenceValue = profiles[profileIndex];
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            if (profile.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(progress);
                if (EditorGUI.EndChangeCheck())
                {
                    prismaticJoint.Progress = progress.floatValue;
                }
            }

            EditorGUILayout.PropertyField(attachmentMode);

            if (attachmentMode.intValue == (int)AttachmentMode.Attached)
            {
                EditorGUILayout.PropertyField(attachedBody);
            }

            serializedObject.ApplyModifiedProperties();

            Tools.hidden = mode != EditMode.None;
        }

        protected virtual void OnSceneGUI()
        {
            if (!Application.isPlaying)
            {
                PrismaticJoint joint = (PrismaticJoint)target;
                Transform transform = joint.transform;

                Vector3 startWorldPos = transform.TransformPoint(joint.start);
                if (mode == EditMode.Start)
                {
                    EditorGUI.BeginChangeCheck();

                    startWorldPos = Handles.PositionHandle(startWorldPos, Quaternion.identity);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(joint, "Move start");
                        joint.start = transform.InverseTransformPoint(startWorldPos);
                    }
                }

                Vector3 endWorldPos = transform.TransformPoint(joint.end);
                if (mode == EditMode.End)
                {
                    EditorGUI.BeginChangeCheck();

                    endWorldPos = Handles.PositionHandle(endWorldPos, Quaternion.identity);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(joint, "Move end");
                        joint.end = transform.InverseTransformPoint(endWorldPos);
                    }
                }

                Handles.color = Color.green * 0.8f;
                Handles.DrawLine(startWorldPos, endWorldPos, 5.0f);

                if (mode != EditMode.Start && Event.current.type == EventType.Repaint)
                {
                    Handles.color = Color.green;
                    Handles.SphereHandleCap(0, startWorldPos, Quaternion.identity, 0.03f, EventType.Repaint);
                }

                if (mode != EditMode.End && Event.current.type == EventType.Repaint)
                {
                    Handles.color = Color.red;
                    Handles.SphereHandleCap(0, endWorldPos, Quaternion.identity, 0.03f, EventType.Repaint);
                }
            }
        }

        private void SetDefaultProjectProfiles()
        {
            serializedObject.Update();
            if (profile.objectReferenceValue == null)
            {
                var profiles = JointsEditorSettings.GetOrCreateSettings().PrismaticJointProfiles;
                profile.objectReferenceValue = profiles[0];
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}