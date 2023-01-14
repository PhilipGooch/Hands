using NBG.Core;
using NBG.LogicGraph;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace NBG.XPBDRope
{
    public static class RopeBuilder
    {
        public static void ClearRope(Rope rope)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(rope.gameObject, "Clear Rope");
            UnityEditor.Undo.RegisterCompleteObjectUndo(rope, "Clear Rope");
#endif
            if (rope.Bones != null)
            {
                foreach (var b in rope.Bones)
                {
                    if (b != null)
                    {
                        DestroyObject(b.gameObject);
                    }
                }
            }

            rope.bones.Clear();

            if (rope.WorldJoints)
            {
                DestroyObject(rope.WorldJoints);
            }

            if (rope.StartBodyJoint)
            {
                DestroyObject(rope.StartBodyJoint);
            }
            if (rope.EndBodyJoint)
            {
                DestroyObject(rope.EndBodyJoint);
            }

#if UNITY_EDITOR
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rope);
#endif
        }

        public static void BuildRope(Rope rope, System.Action<RopeSegment> onSegmentCreated = null, System.Action<Rigidbody> onObjectAttached = null)
        {
#if UNITY_EDITOR
            if (rope.Bones != null && rope.Bones.Count > 0 && rope.Bones[0] != null)
            {
                if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(rope.Bones[0].gameObject))
                {
                    Debug.LogError($"Could not rebuild {rope.transform.name} because the rope segments are part of a prefab instance. You need to rebuild inside the prefab itself.", rope.gameObject);
                    return;
                }
            }
#endif

            var handles = rope.handles;
            if (handles != null && handles.Length < 2)
            {
                Debug.LogError("Please provide at least two handles for the rope generation.", rope.gameObject);
                return;
            }

            foreach (var handle in handles)
            {
                if (handle == null)
                {
                    Debug.LogError("Null handle detected, unable to build rope!", rope.gameObject);
                    return;
                }
            }

            ClearRope(rope);

#if UNITY_EDITOR
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(rope.gameObject, "Build Rope");
            UnityEditor.Undo.RegisterCompleteObjectUndo(rope, "Build Rope");
#endif

            rope.ApplyBuildProfile();

            var ropeCreationListeners = rope.GetComponents<IRopeCreationListener>();
            foreach (var listener in ropeCreationListeners)
            {
                listener.BeforeRopeCreation(rope);
            }

            var segmentCreationListeners = rope.GetComponents<IRopeSegmentCreationListener>();

            int startSegment = 0;

            var ropeLengthBetweenHandles = rope.CalculateRopeLengthFromHandles();

            if (rope.ExtraRopeLength > 0f)
            {
                var endPoint = handles[0].position;
                var previousDirection = (endPoint - handles[1].position).normalized;
                var startPoint = endPoint + previousDirection * rope.ExtraRopeLength;
                BuildSegmentsBetweenPoints(rope, startPoint, endPoint, false, ref startSegment, segmentCreationListeners, onSegmentCreated, false);
            }

            int firstActiveSegment = startSegment;

            for (int h = 1; h < handles.Length; h++)
            {
                var startPos = handles[h - 1].position;
                var endPos = handles[h].position;
                BuildSegmentsBetweenPoints(rope, startPos, endPos, h == handles.Length - 1, ref startSegment, segmentCreationListeners, onSegmentCreated);
            }

            var worldJoints = CreateWorldJointObject(rope.transform);

            var startBone = rope.bones[firstActiveSegment];
            var endBone = rope.bones[rope.LastBoneNumber];

            ConfigurableJoint startJoint = null;
            ConfigurableJoint endJoint = null;

            if (rope.BodyStartIsAttachedTo != null)
            {
                AdjustAttachedBoneInvMass(startBone, rope.BodyStartIsAttachedTo);

                startJoint = AttachSegmentToObject(rope, rope.bones[firstActiveSegment].body, rope.BodyStartIsAttachedTo.gameObject, startBone.GetConnectionPoint());
                onObjectAttached?.Invoke(rope.BodyStartIsAttachedTo);
            }
            else if (rope.fixRopeStart)
            {
                startBone.fixedPosition = true;
                startJoint = AttachSegmentToWorld(rope, worldJoints, startBone.body, startBone.GetConnectionPoint());
            }

            if (rope.BodyEndIsAttachedTo != null)
            {
                AdjustAttachedBoneInvMass(rope.bones[rope.LastBoneNumber], rope.BodyEndIsAttachedTo);

                endJoint = AttachSegmentToObject(rope, endBone.body, rope.BodyEndIsAttachedTo.gameObject, -endBone.GetConnectionPoint());
                onObjectAttached?.Invoke(rope.BodyEndIsAttachedTo);
            }
            else if (rope.fixRopeEnd)
            {
                endBone.fixedPosition = true;
                endJoint = AttachSegmentToWorld(rope, worldJoints, endBone.body, -endBone.GetConnectionPoint());
            }

            rope.SetRopeEndJointsEnabled(true);

            foreach (var listener in ropeCreationListeners)
            {
                listener.AfterRopeCreation(rope);
            }

            rope.UpdateRopeData(startJoint, endJoint, worldJoints);

#if UNITY_EDITOR
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(rope);
#endif
        }

        static void BuildSegmentsBetweenPoints(Rope rope, Vector3 startPos, Vector3 endPos, bool needEndOffset, ref int startSegment, IRopeSegmentCreationListener[] segmentCreationListeners,
            System.Action<RopeSegment> onSegmentCreated, bool segmentEnabled = true)
        {
            var diff = endPos - startPos;
            var direction = diff.normalized;
            // Make the rope start completely overlap the handle position
            var ropeStartEndOffset = rope.Radius - rope.SegmentOverlap * 0.5f;
            startPos -= direction * ropeStartEndOffset;
            if (needEndOffset)
            {
                endPos += direction * ropeStartEndOffset;
            }
            else
            {
                endPos -= direction * ropeStartEndOffset;
            }
            diff = endPos - startPos;
            var length = diff.magnitude;

            var rotation = Quaternion.LookRotation((endPos - startPos).normalized);
            int segmentsToCreate = Mathf.RoundToInt(length / rope.SegmentLength);
            float leftoverLength = length - segmentsToCreate * rope.SegmentLength;
            float extraLengthPerSegment = leftoverLength / segmentsToCreate;
            float finalLength = rope.SegmentLength + extraLengthPerSegment;
            for (int i = startSegment; i < startSegment + segmentsToCreate; i++)
            {
                int pointInCurrentLine = i - startSegment;
                var pos = startPos + (rotation * Vector3.forward) * finalLength * (0.5f + pointInCurrentLine);

                RopeSegment segment = CreateSegmentObject(rope, pos, rotation, rope.transform, finalLength, rope.SegmentOverlap, i, segmentCreationListeners);

                // joint to previous
                if (i != 0)
                {
                    ConnectSegmentsEditor(rope, segment, rope.bones[i - 1]);
                }

                segment.gameObject.SetActive(segmentEnabled);

                onSegmentCreated?.Invoke(segment);
            }

            startSegment += segmentsToCreate;
        }

        static void AdjustAttachedBoneInvMass(RopeSegment bone, Rigidbody attachedBody)
        {
            if (attachedBody.isKinematic)
            {
                bone.fixedPosition = true;
            }
        }

        static void SetupSegmentComponent(Rope rope, RopeSegment segment, Vector3 position, Quaternion rotation, Transform parent, float length)
        {
            segment.transform.SetParent(parent, false);
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(segment.gameObject, "Segment Creation");
#endif
            segment.originalLength = length;
            segment.overlap = rope.SegmentOverlap;
            segment.transform.position = position;
            segment.transform.rotation = rotation;
            segment.body = segment.GetComponent<Rigidbody>();
            segment.radius = rope.Radius;
            segment.capsule = segment.GetComponent<CapsuleCollider>();
        }

        static void ConnectSegmentsEditor(Rope rope, RopeSegment next, RopeSegment current)
        {
            next.SetPreviousSegment(current);
            var joint = AddComponent<ConfigurableJoint>(current.gameObject);
            SetupSegmentConnection(rope, next, current, joint);
        }

        internal static void SetupSegmentConnection(Rope rope, RopeSegment next, RopeSegment current, ConfigurableJoint joint)
        {
            SetupGenericJointSettings(rope, joint);
            joint.connectedBody = next.body;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = rope.BuildProfile.AngularMotion;
            if (rope.BuildProfile.UseTwistLimits)
            {
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.highAngularXLimit = new SoftJointLimit { limit = rope.BuildProfile.TwistLimit };
                joint.lowAngularXLimit = new SoftJointLimit { limit = -rope.BuildProfile.TwistLimit };
            }

            joint.axis = new Vector3(0, 0, 1);
            joint.secondaryAxis = new Vector3(1, 0, 0);
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector3(0, 0, current.BoneLength / 2);
            joint.connectedAnchor = new Vector3(0, 0, -next.BoneLength / 2);
            joint.linearLimitSpring = new SoftJointLimitSpring { spring = rope.BuildProfile.LinearSpring, damper = rope.BuildProfile.LinearDamper };

            // Unity has a bug where a newly created joint will have collisions enabled regardless of the enableCollisions setting
            // This is a workaround to that issue
            // Last checked on 2020.3.16f1
            next.capsule.isTrigger = !next.capsule.isTrigger;
            next.capsule.isTrigger = !next.capsule.isTrigger;

            next.connectionOnPreviousSegment = joint;
            current.connectionToNextSegment = joint;
        }

        static RopeSegment CreateSegmentObject(Rope rope, Vector3 position, Quaternion rotation, Transform parent, float length,
            float overlap, int id, IRopeSegmentCreationListener[] listeners)
        {
            var go = new GameObject();
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Rope Segment");
#endif
            go.name = "bone" + id;
            go.layer = rope.gameObject.layer;
            var capsule = go.AddComponent<CapsuleCollider>();
            // enums not invented at unity HQ
            capsule.direction = 2;
            capsule.radius = rope.Radius;
            capsule.height = length + overlap;
            capsule.sharedMaterial = rope.PhysicMaterial;
            var rig = go.AddComponent<Rigidbody>();
            rig.collisionDetectionMode = rope.CollisionDetectionMode;
            rig.mass = length * rope.MassPerMeter;
            rig.drag = rope.Drag;
            rig.angularDrag = rope.AngularDrag;
            rig.interpolation = rope.Interpolation;

            foreach (var listener in listeners)
            {
                listener.BeforeSegmentCreation(go);
            }

            var segment = go.AddComponent<RopeSegment>();
            SetupSegmentComponent(rope, segment, position, rotation, parent, length);

            foreach (var listener in listeners)
            {
                listener.AfterSegmentCreation(segment);
            }

            segment.CollectAllListeners();

            rope.bones.Add(segment);
            return segment;
        }

        static internal void SetupGenericJointSettings(Rope rope, ConfigurableJoint joint)
        {
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            var mass = joint.GetComponent<Rigidbody>().mass;
            joint.slerpDrive = new JointDrive() { positionSpring = rope.BuildProfile.SlerpSpring, positionDamper = rope.BuildProfile.SlerpDampingScale * mass, maximumForce = float.MaxValue };
        }

        static ConfigurableJoint AttachSegmentToObject(Rope rope, Rigidbody segment, GameObject target, Vector3 anchor)
        {
            var joint = AddComponent<ConfigurableJoint>(target);
            joint.connectedBody = segment;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = anchor;
            var worldAnchor = segment.transform.TransformPoint(anchor);
            joint.anchor = target.transform.InverseTransformPoint(worldAnchor);

            var worldAxis = segment.transform.TransformDirection(new Vector3(0, 0, 1));
            var secondaryWorldAxis = segment.transform.TransformDirection(new Vector3(1, 0, 0));
            joint.axis = target.transform.InverseTransformDirection(worldAxis);
            joint.secondaryAxis = target.transform.InverseTransformDirection(secondaryWorldAxis);

            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
            SetupGenericJointSettings(rope, joint);

            return joint;
        }

        static ConfigurableJoint AttachSegmentToWorld(Rope rope, GameObject worldJoints, Rigidbody segment, Vector3 anchor)
        {
            var joint = AddComponent<ConfigurableJoint>(worldJoints);
            joint.connectedBody = segment;

            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = anchor;
            var worldAnchor = segment.transform.TransformPoint(anchor);
            joint.anchor = worldJoints.transform.InverseTransformPoint(worldAnchor);

            joint.axis = new Vector3(0, 0, 1);
            joint.secondaryAxis = new Vector3(1, 0, 0);

            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;

            SetupGenericJointSettings(rope, joint);
            return joint;
        }

        static GameObject CreateWorldJointObject(Transform parent)
        {
            var worldJoints = new GameObject();
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(worldJoints, "Create World Joint");
#endif
            worldJoints.name = "World Joints";
            worldJoints.transform.parent = parent;
            worldJoints.transform.localPosition = Vector3.zero;
            worldJoints.transform.localRotation = Quaternion.identity;
            var rig = worldJoints.AddComponent<Rigidbody>();
            rig.isKinematic = true;

            return worldJoints;
        }

        static void DestroyObject(Object target)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(target);
#else
            GameObject.Destroy(target);
#endif
        }

        static T AddComponent<T>(GameObject target) where T : Component
        {
#if UNITY_EDITOR
            return UnityEditor.Undo.AddComponent<T>(target);
#else
            return target.AddComponent<T>();
#endif
        }
    }
}