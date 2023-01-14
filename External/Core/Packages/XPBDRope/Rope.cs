using NBG.Core;
using NBG.LogicGraph;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

[assembly: Preserve]
namespace NBG.XPBDRope
{
    /// <summary>
    /// An XPBD Rope. Internally, it creates rope segment rigidbodies, connects them with joints and solves XPBD constraints to stabilize the rope.
    /// This allows for much more stable ropes, at the cost of extra performance.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class Rope : MonoBehaviour, IManagedBehaviour
    {
        [SerializeField]
        RopeProfile ropeProfile;
        //AttachmentSettings
        [SerializeField]
        Rigidbody attachStartTo;
        [SerializeField]
        Rigidbody attachEndTo;
        [SerializeField]
        internal bool fixRopeStart;
        [SerializeField]
        internal bool fixRopeEnd;
        [SerializeField]
        bool fixRopeStartRotation;
        [SerializeField]
        bool fixRopeEndRotation;

        /// <summary>
        /// The thickness radius of the rope.
        /// </summary>
        public float Radius => BuildProfile.Radius;
        public float RendererRadius => ropeProfile.ProfileData.RendererRadius;
        public float SegmentLength => BuildProfile.SegmentLength;
        public float SegmentOverlap => Radius * 2f;
        /// <summary>
        /// Should the rope use twist limits to prevent rope segments from twisting too far apart from each other.
        /// </summary>
        public bool UseTwistLimits => BuildProfile.UseTwistLimits;
        /// <summary>
        /// The twist limit between rope segments in degrees.
        /// </summary>
        public float TwistLimit => BuildProfile.TwistLimit;
        /// <summary>
        /// Controls rope stiffness. The lower the value, the stiffer the rope
        /// Too soft will not pull rope together, too stiff send too much waves
        /// compliance = 0.00000000004f; //  0.04 x 10^(-9) (M^2/N) Concrete
        /// compliance = 0.16 x 10^(-9) (M^2/N) Wood - good for ropes
        /// compliance = 0.0000000005f; // 
        /// compliance = 0.000000001f; // 1.0  x 10^(-8) (M^2/N) Leather
        /// compliance = 0.000000002f;   // 0.2  x 10^(-7) (M^2/N) Tendon
        /// compliance = 0.0000001f,     // 1.0  x 10^(-6) (M^2/N) Rubber
        /// compliance = 0.00002f;       // 0.2  x 10^(-3) (M^2/N) Muscle (too soft)
        /// compliance = 0.0001f;        // 1.0  x 10^(-3) (M^2/N) Fat
        /// </summary>
        public float ElasticCompliance => ropeProfile.ProfileData.ElasticCompliance;
        public float BendCompliance => ropeProfile.ProfileData.BendCompliance;
        public float BendLimit => ropeProfile.ProfileData.BendLimit;

        public float MassPerMeter => BuildProfile.MassPerMeter;
        public float Drag => BuildProfile.Drag;
        public float AngularDrag => BuildProfile.AngularDrag;
        public RigidbodyInterpolation Interpolation => BuildProfile.Interpolation;
        public CollisionDetectionMode CollisionDetectionMode => BuildProfile.CollisionDetectionMode;
        public PhysicMaterial PhysicMaterial => BuildProfile.PhysicMaterial;
        public float LinearSpring => BuildProfile.LinearSpring;
        public float LinearDamper => BuildProfile.LinearDamper;
        public float MaxSegmentSeparation => ropeProfile.ProfileData.MaxSegmentSeparation;
        public float StaticFriction => BuildProfile.StaticFriction;
        public float DynamicFriction => BuildProfile.DynamicFriction;


        [SerializeField]
        [Range(0f, 1f)]
        float ropeLengthMultiplier = 1f;

        /// <summary>
        /// How long should the rope be, relative to it's initial length. Accepts values from 0 to 1.
        /// </summary>
        [NodeAPI("RopeLengthMultiplier", scope: NodeAPIScope.Sim)]
        public float RopeLengthMultiplier
        {
            get
            {
                return ropeLengthMultiplier;
            }
            set
            {
                ropeLengthMultiplier = Mathf.Clamp01(value);
            }
        }

        [SerializeField]
        [FormerlySerializedAs("originalRopeLength")]
        [FormerlySerializedAs("maxRopeLength")]
        float legacyRopeLength = 0f;
        [SerializeField]
        float baseRopeLength = 0f;
        [SerializeField]
        float extraRopeLength = 0f;
        internal float ExtraRopeLength => extraRopeLength;

        float previousLength = 1f;

        [SerializeField]
        internal Transform[] handles;

        [SerializeField]
        [HideInInspector]
        ConfigurableJoint startJoint;
        [SerializeField]
        [HideInInspector]
        ConfigurableJoint endJoint;
        [SerializeField]
        [HideInInspector]
        GameObject worldJoints;
        internal GameObject WorldJoints => worldJoints;
        [SerializeField]
        [HideInInspector]
        // Unity does not serialize nullables so we have to improvise.
        bool hasBuildProfile = false;
        [SerializeField]
        [HideInInspector]
        RopeProfileData buildProfile;
        internal RopeProfileData BuildProfile
        {
            get
            {
                if (!hasBuildProfile)
                {
                    return ropeProfile.ProfileData;
                }
                return buildProfile;
            }
            set
            {
                buildProfile = value;
                hasBuildProfile = true;
            }
        }
        [SerializeField]
        bool useProfileOverride = false;
        [SerializeField]
        RopeProfileOverride profileOverride;

        internal void ApplyBuildProfile()
        {
            BuildProfile = ropeProfile.ProfileData;
            if (useProfileOverride)
            {
                BuildProfile = buildProfile.ApplyOverride(profileOverride);
            }
        }

        /// <summary>
        /// How long is the whole rope in units.
        /// </summary>
        public float MaxRopeLength { get { return baseRopeLength + extraRopeLength; } }

        float MinRopeLength => Radius * 2f + SegmentOverlap;

        /// <summary>
        /// How long is the rope right now, accounting for the rope length multiplier.
        /// </summary>
        public float CurrentRopeLength
        {
            get
            {
                if (ActiveBoneCount > 0)
                {
                    return Mathf.Max(MaxRopeLength * RopeLengthMultiplier, MinRopeLength);
                }
                return 0f;
            }
        }

        [SerializeField]
        internal List<RopeSegment> bones = new List<RopeSegment>();
        public IReadOnlyList<RopeSegment> Bones => bones.AsReadOnly();

        public int ActiveBoneCount { get; private set; }
        public int FirstActiveBone => BoneCount - ActiveBoneCount;
        public int BoneCount => bones.Count;

        public System.Action<RopeSegment> onStartSegmentChanged;
        public bool NeedsRegeneration { get; private set; } = false;
        public RopeSegment ActiveStartSegment { get; private set; }

        public bool RopeInitialized { get; private set; } = false;
        private bool ropeRegisteredToManager = false;

        public Rigidbody BodyStartIsAttachedTo => attachStartTo;
        public Rigidbody BodyEndIsAttachedTo => attachEndTo;
        public ConfigurableJoint StartBodyJoint => startJoint;
        public ConfigurableJoint EndBodyJoint => endJoint;

        List<IRopeSolveListener> solveListeners = new List<IRopeSolveListener>();

        internal int LastBoneNumber => bones.Count - 1;

        RopeStartConnector startConnector = new RopeStartConnector();
        IgnoreRopeSegmentCollision ignoreStartCollision;
        IgnoreRopeSegmentCollision IgnoreStartCollision
        {
            get
            {
                if (ignoreStartCollision == null)
                {
                    ignoreStartCollision = new IgnoreRopeSegmentCollision(BodyStartIsAttachedTo);
                }
                return ignoreStartCollision;
            }
        }

        [SerializeField]
        [HideInInspector]
        internal int version = 0;
        public int Version => version;
        public const int latestRopeVersion = 3;
        // Version 1: extendable rope rework
        // Version 2: enforce 2*radius segment overlap for all ropes
        // Version 3: rope profiles

#if UNITY_EDITOR
        // Used by the validation test to perform upgrades
        public Rigidbody AttachStartTo { get { return attachStartTo; } set { attachStartTo = value; } }
        public Rigidbody AttachEndTo { get { return attachEndTo; } set { attachEndTo = value; } }
        public Transform[] Handles { get { return handles; } set { handles = value; } }

        public bool IsOutdated => version < latestRopeVersion;

        public bool IsBuilt => bones != null && bones.Count > 0 && bones[0] != null;

        public string RopeDeltaReport 
        { 
            get
            {
                if (ropeProfile == null)
                {
                    return "This rope needs to be rebuilt to function properly - no build was data found.";
                }
                var currentProfile = ropeProfile.ProfileData;
                if (useProfileOverride)
                {
                    currentProfile = currentProfile.ApplyOverride(profileOverride);
                }
                return RopeProfileData.GetComparisonReport(BuildProfile, currentProfile);
            }
        }

        [ContextMenu("Clear Rope")]
        public void ClearRope()
        {
            RopeBuilder.ClearRope(this);
        }

        // ContextMenu does not support functions with default parameters
        [ContextMenu("Build Rope")]
        void BuildRopeInternal()
        {
            RopeBuilder.BuildRope(this);
        }

        public void ResetLength()
        {
            UnityEditor.Undo.RegisterCompleteObjectUndo(this, "Reset rope length");
            RecalculateBaseLength();
            extraRopeLength = 0f;
            ropeLengthMultiplier = 1f;
        }
#endif
        public void UpdateRopeData(ConfigurableJoint startJoint, ConfigurableJoint endJoint, GameObject worldJoints)
        {
            this.startJoint = startJoint;
            this.endJoint = endJoint;
            this.worldJoints = worldJoints;
            GatherRopeSolveListeners();
            version = latestRopeVersion;
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            InitializeBones();
            // If the rope is disabled, make the end joints inactive
            SetRopeEndJointsEnabled(isActiveAndEnabled);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            TryRegisterRope();
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            DeinitializeBones();
        }

        private void TryRegisterRope()
        {
            if (!RopeInitialized || ropeRegisteredToManager || !isActiveAndEnabled)
                return;

            var system = RopeSystem.Instance;
            if (system)
            {
                system.AddRope(this);
            }
            else
            {
                Debug.LogError("No rope system found! Please add a rope system to the scene.", gameObject);
            }

            ropeRegisteredToManager = true;
        }

        private void TryUnregisterRope()
        {
            if (!RopeInitialized || !ropeRegisteredToManager)
                return;

            var system = RopeSystem.Instance;
            if (system)
            {
                system.RemoveRope(this);
            }

            ropeRegisteredToManager = false;
        }

        private void OnEnable()
        {
            SetRopeEndJointsEnabled(true);
            MarkRopeDirty();
            if (version > 0)
            {
                startConnector.Enable(this, startJoint, fixRopeStart);
            }
            TryRegisterRope();
        }

        private void OnDisable()
        {
            SetRopeEndJointsEnabled(false);
            if (version > 0)
            {
                startConnector.Disable();
            }
            TryUnregisterRope();
        }

        void InitializeBones()
        {
            if (RopeInitialized)
                return;

            foreach (var bone in bones)
            {
                bone.Initialize(MarkRopeDirty);
            }

            RopeInitialized = true;
        }

        void DeinitializeBones()
        {
            if (!RopeInitialized)
                return;

            RopeInitialized = false;
            foreach (var bone in bones)
            {
                bone.Deinitialize();
            }
        }

        void ConfigureJointMotion(ConfigurableJoint joint, bool fixLinear, bool fixAngular)
        {
            joint.xMotion = joint.yMotion = joint.zMotion = fixLinear ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = fixAngular ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        }

        // Rope end joints need to be free if the rope segments are disabled to prevent moving connected bodies to 0,0,0
        internal void SetRopeEndJointsEnabled(bool enabled)
        {
            if (startJoint)
            {
                // Must set to null, otherwise the joint will teleport to origin when it becomes active again
                startJoint.connectedBody = enabled ? bones[0].body : null;
                ConfigureJointMotion(startJoint, enabled, enabled && fixRopeStartRotation);
            }
            if (endJoint)
            {
                // Must set to null, otherwise the joint will teleport to origin when it becomes active again
                endJoint.connectedBody = enabled ? bones[LastBoneNumber].body : null;
                ConfigureJointMotion(endJoint, enabled, enabled && fixRopeEndRotation);
            }
        }

        void MarkRopeDirty()
        {
            NeedsRegeneration = true;
        }

        public void Regenerate()
        {
            AdjustRopeLength();
            NeedsRegeneration = false;
        }

        void ConnectSegments(RopeSegment next, RopeSegment current)
        {
            var joint = current.connectionToNextSegment;
            RopeBuilder.SetupSegmentConnection(this, next, current, joint);
        }

        private void FixedUpdate()
        {
            if (ropeLengthMultiplier != previousLength)
            {
                MarkRopeDirty();
            }

            for(int i = 0; i < bones.Count - 1; i++)
            {
                if (!bones[i].connectionToNextSegment)
                {
                    Debug.LogError("Missing rope segment joint! Was a rope joint destroyed? That is not supported, rope joints should never be destroyed. Rope will be disabled.", bones[i]);
                    gameObject.SetActive(false);
                }
            }
        }

        void AdjustRopeLength()
        {
            if (version > 0)
            {
                var wantedLength = Mathf.Lerp(MinRopeLength, MaxRopeLength, ropeLengthMultiplier);
                var accumulatedLength = SegmentOverlap;
                for (int i = LastBoneNumber; i >= 0; i--)
                {
                    var bone = bones[i];
                    accumulatedLength += bone.originalLength;

                    if (accumulatedLength < wantedLength)
                    {
                        bone.ActivateSegment();
                        ActiveStartSegment = bone;
                        bone.SetLengthMultiplier(1f);
                    }
                    else
                    {
                        var lengthDiff = accumulatedLength - wantedLength;
                        // We're multiplying two floats and comparing them against a sum of other floats.
                        // Allow a small error value to account for that.
                        const float maxError = 0.0001f;
                        if (lengthDiff >= bone.originalLength - maxError)
                        {
                            bone.DeactivateSegment();
                        }
                        else
                        {
                            bone.ActivateSegment();
                            ActiveStartSegment = bone;
                            var invertedDiff = bone.originalLength - lengthDiff;
                            bone.SetLengthMultiplier(invertedDiff / bone.originalLength);
                        }
                    }
                }

                previousLength = ropeLengthMultiplier;

                ActiveBoneCount = 0;
                for (int i = LastBoneNumber; i >= 0; i--)
                {
                    var bone = bones[i];
                    if (bone.IsActive)
                    {
                        ActiveBoneCount++;

                        if (bone.NeedsReconnection && bone.Next != null && bone.Next.IsActive)
                        {
                            bone.ReconnectSegment();
                            ConnectSegments(bone.Next, bone);
                        }
                    }
                }

                if (endJoint != null)
                {
                    endJoint.autoConfigureConnectedAnchor = false;
                    endJoint.connectedAnchor = -bones[LastBoneNumber].GetConnectionPoint();
                }

                var secondActiveBone = FirstActiveBone + 1;
                IgnoreStartCollision.UpdateCollisionIgnore(secondActiveBone < BoneCount ? bones[secondActiveBone] : null);
            }
            else
            {
                for(int i = 0; i < bones.Count;i++)
                {
                    bones[i].ActivateSegment();
                }
                ActiveStartSegment = bones[0];
                previousLength = 1f;
                ropeLengthMultiplier = 1f;
                ActiveBoneCount = bones.Count;
            }

            onStartSegmentChanged?.Invoke(ActiveStartSegment);
        }

        internal float CalculateRopeLengthFromHandles()
        {
            float length = 0f;
            if (handles != null)
            {
                for (int i = 1; i < handles.Length; i++)
                {
                    if (handles[i] != null && handles[i-1] != null)
                    {
                        length += (handles[i].position - handles[i - 1].position).magnitude;
                    }
                }
            }
            return length + Radius * 2f;
        }

        public void BeforeSolve()
        {
            for (int i = 0; i < BoneCount; i++)
            {
                if (bones[i].IsActive)
                {
                    bones[i].BeforeRopeSolve();
                }
            }

            foreach (var listener in solveListeners)
            {
                listener.BeforeRopeSolve(this);
            }
        }

        public float GetPointInvMass(int targetPoint)
        {
            // Disabled bones have inv mass of 0.
            if (targetPoint < FirstActiveBone)
                return 0f;

            var targetBone = bones[BoneCount - 1];

            if (targetPoint < BoneCount)
            {
                targetBone = bones[targetPoint];
            }

            var invMass = targetBone.GetInvMass();

            if (targetPoint == FirstActiveBone || targetPoint == BoneCount) // First or last point
            {
                invMass *= 2f;
            }
            else // Mid point
            {
                invMass += bones[targetPoint - 1].GetInvMass();
                invMass /= 2f;
            }

            return invMass;
        }

        public void AfterSolve()
        {
            for (int i = 0; i < BoneCount; i++)
            {
                if (bones[i].IsActive)
                {
                    bones[i].AfterRopeSolve();
                }
            }

            foreach (var listener in solveListeners)
            {
                listener.AfterRopeSolve(this);
            }
        }

        internal void GatherRopeSolveListeners()
        {
            solveListeners.Clear();
            GetComponents(solveListeners);
        }

        void OnValidate()
        {
            MakeSureRopeProfileIsSelected();
            if (useProfileOverride)
            {
                profileOverride.InitializeDefaults(ropeProfile.ProfileData);
            }

            WriteDefaultBuildProfile();
            GatherRopeSolveListeners();

            // Bones can be null OnValidate when redoing a rope build for some reason
            if (bones.Count > 0 && bones[0])
            {
                EnsureJointExists(ref startJoint, attachStartTo, bones[0]);
                EnsureJointExists(ref endJoint, attachEndTo, bones[bones.Count - 1]);
            }

            AdjustRopeLengthModifier();

            Debug.Assert(GetComponents<Rope>().Length == 1, "Multiple Rope components on a single object detected!", gameObject);
        }

        void WriteDefaultBuildProfile()
        {
            if (!hasBuildProfile && ropeProfile != null)
            {
                BuildProfile = ropeProfile.ProfileData;
                if (useProfileOverride)
                {
                    BuildProfile = buildProfile.ApplyOverride(profileOverride);
                }
            }
        }

        void EnsureJointExists(ref ConfigurableJoint joint, Rigidbody targetBody, RopeSegment targetSegment)
        {
            if (targetBody != null && joint == null)
            {
                joint = FindJointConnectedToBody(targetSegment, targetBody);
                if (joint == null)
                {
                    Debug.LogError($"No joint found to connect {transform.name} to {targetBody.name}. Rebuild the rope!", gameObject);
                }
            }
        }

        ConfigurableJoint FindJointConnectedToBody(RopeSegment targetSegment, Rigidbody targetBody)
        {
            var joints = targetSegment.GetComponents<ConfigurableJoint>();
            foreach (var joint in joints)
            {
                if (joint.connectedBody == targetBody)
                {
                    return joint;
                }
            }
            return null;
        }

        void AdjustRopeLengthModifier()
        {
            if (!Application.isPlaying && handles != null && handles.Length > 1)
            {
                foreach (var handle in handles)
                {
                    // Don't do anything if the rope handles are not properly set up.
                    if (handle == null)
                        return;
                }
                RecalculateBaseLength();
                // Legacy max rope length data detected
                if (legacyRopeLength > 0f)
                {
                    extraRopeLength = legacyRopeLength - baseRopeLength;
                    legacyRopeLength = 0f;
                }

                if (extraRopeLength > 0f)
                {
                    ropeLengthMultiplier = Mathf.InverseLerp(MinRopeLength, baseRopeLength + extraRopeLength, baseRopeLength);
                }
            }
        }

        public void RecalculateBaseLength()
        {
            baseRopeLength = CalculateRopeLengthFromHandles();
        }

        void MakeSureRopeProfileIsSelected()
        {
#if UNITY_EDITOR
            const string defaultGUID = "4992eeb0a11c5204faa58778f7817533";
            if (ropeProfile == null)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(defaultGUID);
                ropeProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<RopeProfile>(path);
            }
#endif
        }

        private void OnDrawGizmos()
        {
            Transform previousHandle = null;
            if (handles != null)
            {
                foreach (var handle in handles)
                {
                    if (handle != null)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(handle.position, Radius);
                        if (previousHandle != null)
                        {
                            Gizmos.DrawLine(previousHandle.position, handle.position);
                        }
                        previousHandle = handle;
                    }
                }

                if (handles.Length > 1)
                {
                    var firstHandle = handles[0];
                    var secondHandle = handles[1];
                    if (firstHandle != null && secondHandle != null)
                    {
                        if (extraRopeLength > 0f)
                        {
                            var lastPos = firstHandle.position;
                            var lastDir = (lastPos - secondHandle.position).normalized;
                            var nextPos = lastPos + lastDir * extraRopeLength;
                            Gizmos.color = Color.red * 0.75f;
                            Gizmos.DrawWireSphere(nextPos, Radius);
                            Gizmos.DrawLine(lastPos, nextPos);
                        }
                    }
                }
            }
        }
    }
}