using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NBG.Joints
{
    /// <summary>
    /// Custom inspector for revolute Joint. It renders gizmos and hides unused parameters.
    /// </summary>
    [CustomEditor(typeof(RevoluteJoint)), CanEditMultipleObjects]
    public class RevoluteJointEditor : Editor
    {
        private SerializedProperty pivot, axis, rotationStart, maxAngle, minAngle, useLimits, useProgress, progress, useMotor, force, targetVelocity, profile, attachmentMode, attachedBody;

        private const float lineSize = 0.6f;
        private const float arcRadius = 0.5f;
        private const float sphereRadius = 0.03f;
        private const float lineThickness = 5.0f;

        public enum EditMode
        {
            None,
            Pivot,
            Axis,
            RotationStart
        }

        private EditMode mode = EditMode.None;

        private void OnEnable()
        {
            pivot = serializedObject.FindProperty("pivot");
            axis = serializedObject.FindProperty("axis");
            rotationStart = serializedObject.FindProperty("rotationStart");
            maxAngle = serializedObject.FindProperty("maxAngle");
            minAngle = serializedObject.FindProperty("minAngle");
            minAngle = serializedObject.FindProperty("minAngle");
            useLimits = serializedObject.FindProperty("useLimits");

            useProgress = serializedObject.FindProperty("useProgress");
            progress = serializedObject.FindProperty("progress");

            useMotor = serializedObject.FindProperty("useMotor");
            force = serializedObject.FindProperty("force");
            targetVelocity = serializedObject.FindProperty("targetVelocity");

            attachedBody = serializedObject.FindProperty("attachedBody");
            attachmentMode = serializedObject.FindProperty("attachmentMode");

            profile = serializedObject.FindProperty("profile");

            SetDefaultProjectProfiles();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditableVector(pivot, EditMode.Pivot);
            if (EditableVector(axis, EditMode.Axis))
            {
                axis.vector3Value = axis.vector3Value.normalized;
                rotationStart.vector3Value = RevoluteJoint.FixRightAxis(axis.vector3Value, rotationStart.vector3Value);
            }

            EditorGUILayout.PropertyField(useLimits);
            if (useLimits.boolValue)
            {
                EditorGUI.indentLevel++;

                if (EditableVector(rotationStart, EditMode.RotationStart))
                    rotationStart.vector3Value = RevoluteJoint.FixRightAxis(axis.vector3Value, rotationStart.vector3Value);

                EditorGUILayout.PropertyField(maxAngle);
                EditorGUILayout.PropertyField(minAngle);

                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.PropertyField(useProgress);

                if (useProgress.boolValue)
                {
                    EditorGUI.indentLevel++;

                    string[] profileOptions;
                    int profileIndex = 0;
                    try
                    {
                        var profiles = JointsEditorSettings.GetOrCreateSettings().RevoluteJointProfiles;
                        profileOptions = profiles.Select(e => e.ProfileName).ToArray();
                        profileIndex = profiles.FindIndex(e => e == profile.objectReferenceValue);
                    }
                    catch
                    {
                        Debug.LogError("Project joint profiles file is empty");
                        return;
                    }

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUI.BeginChangeCheck();
                        profileIndex = EditorGUILayout.Popup("Spring mode", profileIndex, profileOptions);

                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.PropertyField(profile, GUIContent.none, GUILayout.Width(64)); // Debug reference
                        EditorGUI.EndDisabledGroup();

                        if (EditorGUI.EndChangeCheck())
                        {
                            var profiles = JointsEditorSettings.GetOrCreateSettings().RevoluteJointProfiles;
                            profile.objectReferenceValue = profiles[profileIndex];
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(progress);
                    if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                    {
                        RevoluteJoint joint = (RevoluteJoint)target;
                        joint.Progress = progress.floatValue;
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(useMotor);
            if (useMotor.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(force);
                EditorGUILayout.PropertyField(targetVelocity);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(attachmentMode);

            if (attachmentMode.intValue == (int)AttachmentMode.Attached)
            {
                EditorGUILayout.PropertyField(attachedBody);
            }

            serializedObject.ApplyModifiedProperties();

            Tools.hidden = mode != EditMode.None;
        }

        private bool EditableVector(SerializedProperty property, EditMode buttonMode)
        {
            bool changed;
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(property);
                changed = EditorGUI.EndChangeCheck();
                if (GUILayout.Button(mode != buttonMode ? "Edit" : "Stop edit"))
                {
                    mode = mode != buttonMode ? buttonMode : EditMode.None;
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();
            return changed;
        }

        protected virtual void OnSceneGUI()
        {
            if (!Application.isPlaying)
            {

                RevoluteJoint joint = (RevoluteJoint)target;
                Transform transform = joint.transform;

                float handlesSize = HandleUtility.GetHandleSize(transform.position) * 3.0f;

                Vector3 worldPivot = transform.TransformPoint(joint.pivot);

                if (mode == EditMode.Pivot)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 pivotNewValue = PositionHandleWorld(joint.pivot * handlesSize) / handlesSize;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(joint, "Moving setting");
                        joint.pivot = pivotNewValue;
                    }
                }

                if (mode == EditMode.RotationStart)
                {
                    EditorGUI.BeginChangeCheck();

                    Vector3 rotationStartNewValue = Quaternion.Inverse(transform.rotation) * (Handles.PositionHandle(transform.rotation * joint.rotationStart * handlesSize + worldPivot, Quaternion.identity) - worldPivot) / handlesSize;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(joint, "Moving setting");
                        joint.rotationStart = RevoluteJoint.FixRightAxis(joint.axis, rotationStartNewValue);
                    }
                }

                if (mode == EditMode.Axis)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 axisNewValue = Quaternion.Inverse(transform.rotation) * (Handles.PositionHandle(transform.rotation * joint.axis * handlesSize + worldPivot, Quaternion.identity) - worldPivot) / handlesSize;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(joint, "Moving setting");
                        joint.axis = axisNewValue.normalized;
                        joint.rotationStart = RevoluteJoint.FixRightAxis(joint.axis, joint.rotationStart);
                    }
                }

                Color orange = new Color(1.0f, 0.3f, 0.0f, 1.0f);
                Vector3 rotatedRotationStart = transform.rotation * joint.rotationStart;
                Vector3 rotatedAxis = transform.rotation * joint.axis;

                if (joint.useLimits)
                {
                    Handles.color = orange * 0.6f;
                    Handles.DrawSolidArc(worldPivot, rotatedAxis, rotatedRotationStart, joint.maxAngle, arcRadius * handlesSize);

                    Handles.color = orange * 0.3f;
                    Handles.DrawSolidArc(worldPivot, rotatedAxis, rotatedRotationStart, joint.minAngle, arcRadius * handlesSize);

                    Handles.color = orange;
                    Handles.DrawLine(worldPivot, worldPivot + rotatedRotationStart * handlesSize * lineSize, lineThickness);
                    Handles.SphereHandleCap(0, worldPivot + rotatedRotationStart * handlesSize * lineSize, Quaternion.identity, sphereRadius * handlesSize, EventType.Repaint);
                }

                Handles.color = Color.yellow * 0.8f;
                Handles.DrawLine(worldPivot, worldPivot + rotatedAxis * handlesSize * lineSize, lineThickness);
                Handles.SphereHandleCap(0, worldPivot + rotatedAxis * handlesSize * lineSize, Quaternion.identity, sphereRadius * handlesSize, EventType.Repaint);

                Handles.color = orange;
                Handles.DrawWireDisc(worldPivot, rotatedAxis, arcRadius * handlesSize);

            }
        }

        public Vector3 PositionHandleWorld(Vector3 pos)
        {
            RevoluteJoint joint = (RevoluteJoint)target;
            Transform transform = joint.transform;
            return transform.InverseTransformPoint(Handles.PositionHandle(transform.TransformPoint(pos), Quaternion.identity));
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