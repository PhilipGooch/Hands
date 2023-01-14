using NBG.Core;
using NBG.Entities;
using NBG.LogicGraph;
using Recoil;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Plugs
{
    public enum HoleType
    {
        FreeZRotation, // single axis - could be rotated freely aroundZ
        Double, // two pins, can be flipped
        Fixed, // directional slot
        NoConstraints, // marble or other object having single constraint
        FreeXRotation, // rotate around x
    }

    /// <summary>
    /// Guide constraint data
    /// </summary>
    public struct HoleSlotConstraintData
    {
        public bool isConnected;
        public RigidTransform relativeHoleX;
        public RigidTransform relativePlugX;
        public float offsetMin;
        public float offsetMax;

        public float depth;
        public float angle;
        public float width;
        //public float3 normal;
    }

    /// <summary>
    /// Guides constraints data
    /// </summary>
    public struct HoleFunnelConstraintData
    {
        public bool bothAxes;
        public bool isCreated;
        public bool normalsDirty;
        public HoleSlotConstraintData slotH;
        public HoleSlotConstraintData slotV;
        public float disengageDist;
    }

    /// <summary>
    /// Plug events
    /// </summary>
    public enum PlugAndHoleEvent
    {
        None,
        CreateGuides, // ensure guides 
        CreateSnap, // ensure guides & snap
        DestroySnap, // ensure no snap, but guides
        DestroyGuides, // full disconnect
        UpdateNormals // preserve connectivity but update normals
    }

    /// <summary>
    /// Active plug and socket data
    /// </summary>
    public struct PlugAndHoleData
    {
        public HoleType holeType;
        public float width;
        public float height;
        public PlugAndHoleEvent syncEvent;

        public int plugBody;
        public int holeBody;
        public RigidTransform relativePlugX;
        public RigidTransform relativeHoleX;

        public bool engageGuides;
        public bool engaged;
        public bool allowSnap;
        public bool snapped;

        public HoleFunnelConstraintData tip1;
        public HoleFunnelConstraintData tip2;
        public HoleFunnelConstraintData plugBase;

        public float maxAngle;
        public float plugDist;
        public float unplugDist;

        public bool isSingle => holeType == HoleType.FreeZRotation || holeType == HoleType.NoConstraints;
        public bool hasBase => holeType != HoleType.NoConstraints && holeType != HoleType.FreeXRotation;
    }

    /// <summary>
    /// Plug controller manages plugging, guiding and snapping to the socket
    /// </summary>
    public class Plug : MonoBehaviour, IManagedBehaviour
    {
        Rigidbody body;
        public Rigidbody Body
        {
            get
            {
                if (body == null)
                    EnsureRigidbodyInit();
                Debug.Assert(body != null, $"{nameof(Plug)} found without RigidBody!", this);

                return body;
            }
        }

        int bodyId = -1;
        public int BodyId
        {
            get
            {
                if (bodyId == World.environmentId)
                    EnsureRigidbodyInit();
                Debug.Assert(body != null, $"{nameof(Plug)} found without RigidBody!", this);

                return bodyId;
            }
        }

        Hole activeHole;
        public Hole ActiveHole => activeHole;

        [SerializeField]
        HoleType holeType;
        public HoleType HoleType => holeType;

        [Tooltip("Allows overriding the center and orientation of the plug.")]
        [SerializeField] Transform alignTransform;

        //500 was a default value, better stay as such
        [SerializeField]
        float snapJointSpring = 500;
        //Cannot be 0 since its used in division
        const float kMinSnapJointSpring = 0.00001f;
        public float SnapJointSpring
        {
            get
            {
                return snapJointSpring;
            }
            set
            {
                snapJointSpring = value;
                if (snapJointSpring == 0)
                    snapJointSpring = kMinSnapJointSpring;

                SetSnapJointSpring(snapJointSpring);
            }
        }

        // state
        public Entity connectionEntity;
        public HoleFunnelConstraint pin1tip = new HoleFunnelConstraint();
        public HoleFunnelConstraint pin2tip = new HoleFunnelConstraint();
        public HoleFunnelConstraint plugBase = new HoleFunnelConstraint();
        public HoleSnapConstraint snap;

        public event Action onEngageGuides;
        public event Action onDisengageGuides;
        [NodeAPI("OnPlugIn")]
        public event Action<Hole> onPlugIn;
        [NodeAPI("OnPlugOut")]
        public event Action<Hole> onPlugOut;
        
        bool snapped;
        [NodeAPI("IsPluggedIn")]
        public bool IsPluggedIn => snapped;

        static EntityArchetype connectionArchetype;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            connectionArchetype = default;
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            EnsureRigidbodyInit();
        }

        void EnsureRigidbodyInit()
        {
            body = GetComponentInParent<Rigidbody>();
            if (body != null)
            {
                bodyId = ManagedWorld.main.FindBody(body);
                Debug.Assert(bodyId != World.environmentId, $"{nameof(Plug)} Rigidbody not yet registered", this);
                snap = new HoleSnapConstraint(snapJointSpring);
            }
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded() { }

        public RigidTransform GetRigidWorldTransform()
        {
            return (alignTransform != null ? alignTransform : transform).GetRigidWorldTransform();
        }

        public void Connect(Hole hole)
        {
            if (activeHole != null)
                throw new System.InvalidOperationException("Trying to plug a plug that's already in hole.");
            activeHole = hole;

            if (connectionArchetype.isEmpty)
            {
                var typeList = ComponentTypeList.Create();
                typeList.AddType<PlugAndHoleData>();
                connectionArchetype = EntityStore.RegisterArchetype(500, typeList);
            }
            connectionEntity = EntityStore.AddEntity(connectionArchetype);
            EntityStore.AddComponentObject(connectionEntity, this);
            ref var data = ref EntityStore.GetComponentData<PlugAndHoleData>(connectionEntity);
            data.holeType = HoleType;
            data.engageGuides = hole.EngageDeepGuides;
            data.plugBody = BodyId;
            data.holeBody = hole.bodyId;
            data.width = data.isSingle ? 0 : .25f;
            data.height = hole.EngageDeepGuides ? .25f : 0;
            data.engaged = false;
            data.snapped = false;

            if (data.holeType == HoleType.FreeXRotation || data.holeType == HoleType.NoConstraints || hole.PreventSnap)
                data.allowSnap = false;
            else
                data.allowSnap = true;

            data.relativePlugX = re.invmul(Body.GetRigidTransform(), GetRigidWorldTransform());
            data.relativeHoleX = re.invmul(hole.body.GetRigidTransform(), hole.GetRigidWorldTransform());
            data.maxAngle = hole.EngageStartMaxAngle;
            data.plugDist = hole.PlugDist;
            data.unplugDist = hole.UnplugDist;

            var alignHeight = .5f;

            var pin1X = math.mul(data.relativePlugX, RigidTransform.Translate(new float3(-data.width, 0, data.height)));
            var pin2X = math.mul(data.relativePlugX, RigidTransform.Translate(new float3(+data.width, 0, data.height)));
            var hol1X = math.mul(data.relativeHoleX, RigidTransform.Translate(new float3(-data.width, 0, 0)));
            var hol2X = math.mul(data.relativeHoleX, RigidTransform.Translate(new float3(+data.width, 0, 0)));
            var pinBase = math.mul(data.relativePlugX, RigidTransform.Translate(new float3(0, 0, data.height - alignHeight)));
            var holBase = math.mul(data.relativeHoleX, RigidTransform.Translate(new float3(0, 0, data.height - alignHeight)));

            pin1tip.Create(ref data.tip1, pin1X, hol1X, true, hole.DisengageDistance);
            if (!data.isSingle) pin2tip.Create(ref data.tip2, pin2X, hol2X, true, hole.DisengageDistance);
            if (data.hasBase) plugBase.Create(ref data.plugBase, pinBase, holBase, true, hole.DisengageDistance);

            CollectEvents(ref data);
            ExecuteEvents();
        }

        void SetSnapJointSpring(float snapJointSpring)
        {
            snap.SetJointSpring(snapJointSpring);
        }

        public void ForceDestroySnapAndGuides()
        {
            if (!connectionEntity.isNull)
            {
                if (EntityStore.TryGetEntityReference(connectionEntity, out var entityRef))
                {
                    ref var data = ref EntityStore.GetComponentData<PlugAndHoleData>(connectionEntity);

                    if (data.engaged)
                    {
                        //all data resets when plug enters socket area again
                        data.unplugDist = float.MinValue;
                        data.plugDist = float.MinValue;

                        data.tip1.disengageDist = float.MinValue;
                        if (!data.tip2.isCreated) data.tip2.disengageDist = float.MinValue;
                    }
                }
            }
        }

        public void LockSnapJointLinearMovement()
        {
            snap.LockJointLinearMovement();
        }

        public void UnlockSnapJointLinearMovement()
        {
            snap.UnlockJointLinearMovement();
        }

        public void LockSnapJointAngularMovement()
        {
            snap.LockJointAngularMovement();
        }

        public void UnlockSnapJointAngularMovement()
        {
            snap.UnlockJointAngularMovement();
        }

        public static void ApplyPins(ref PlugAndHoleData data, bool flip)
        {
            var pin1X = math.mul(data.relativePlugX, RigidTransform.Translate(new float3(-data.width, 0, data.height)));
            var pin2X = math.mul(data.relativePlugX, RigidTransform.Translate(new float3(+data.width, 0, data.height)));
            data.tip1.slotH.relativePlugX = data.tip1.slotV.relativePlugX = flip ? pin2X : pin1X;
            data.tip2.slotH.relativePlugX = data.tip2.slotV.relativePlugX = flip ? pin1X : pin2X;
        }

        public void Disconnect(Hole hole)
        {
            if (activeHole != hole)
                throw new System.InvalidOperationException("Trying to unplug a plug from hole that it's not connected to.");
            activeHole = null;
            ref var data = ref EntityStore.GetComponentData<PlugAndHoleData>(connectionEntity);

            snap.Destroy(ref data);
            pin1tip.Destroy(ref data.tip1);
            pin2tip.Destroy(ref data.tip2);
            plugBase.Destroy(ref data.plugBase);
            EntityStore.RemoveEntity(connectionEntity);
        }

        //private void FixedUpdate()
        //{
        //    if (activeSocket == null) return;
        //    //BuildConstraints();
        //    //CalculateConstraints(ref EntityStore.GetComponentData<PlugAndSocketData>(connectionEntity));
        //    //WriteConstraints();
        //    CollectEvents(ref EntityStore.GetComponentData<PlugAndSocketData>(connectionEntity));
        //    ExecuteEvents();
        //}

        public static void CollectEvents(ref PlugAndHoleData data)
        {
            data.syncEvent = PlugAndHoleEvent.None;
            var plugBodyX = World.main.GetTransformPosition(data.plugBody);
            var holeBodyX = World.main.GetTransformPosition(data.holeBody);
            var plugWorldX = math.mul(plugBodyX, data.relativePlugX);
            var holeWorldX = math.mul(holeBodyX, data.relativeHoleX);

            // detect if joint needs to be created
            if (!data.engaged)
            {
                if (data.hasBase)
                {
                    var dotZ = math.dot(math.rotate(plugWorldX, re.forward), math.rotate(holeWorldX, re.forward));
                    //all data.HoleFunnelConstraintData have the same maxAngle
                    if (dotZ < math.cos(math.radians(data.maxAngle))) return; // z not aligned
                }
                if (!data.isSingle)
                {
                    var dotX = math.dot(math.rotate(plugWorldX, re.right), math.rotate(holeWorldX, re.right));

                    // for double sockets flip pins to align with direction
                    if (data.holeType == HoleType.Double)
                    {
                        ApplyPins(ref data, dotX < 0);
                        dotX = math.abs(dotX);
                    }

                    //all data.HoleFunnelConstraintData have the same maxAngle
                    if (dotX < math.cos(math.radians(data.maxAngle))) return; // x not aligned
                }
                if (HoleFunnelConstraint.ShouldEngage(data.tip1, plugBodyX, holeBodyX) && (!data.tip2.isCreated || HoleFunnelConstraint.ShouldEngage(data.tip2, plugBodyX, holeBodyX)))
                {
                    data.syncEvent = PlugAndHoleEvent.CreateGuides;
                }
            }

            // detect if joint needs to be destroyed
            if (data.engaged)
            {
                if (HoleFunnelConstraint.ShouldDisengage(data.tip1, plugBodyX, holeBodyX) || data.tip2.isCreated && HoleFunnelConstraint.ShouldDisengage(data.tip2, plugBodyX, holeBodyX))
                {
                    data.syncEvent = PlugAndHoleEvent.DestroyGuides;
                }
            }

            // calculate normals
            if (data.engaged && data.syncEvent != PlugAndHoleEvent.DestroyGuides)
            {
                // process normals
                HoleFunnelConstraint.CalculateConstraints(ref data.tip1, plugBodyX, holeBodyX);
                HoleFunnelConstraint.CalculateConstraints(ref data.tip2, plugBodyX, holeBodyX);
                HoleFunnelConstraint.CalculateConstraints(ref data.plugBase, plugBodyX, holeBodyX);

                // mark to update normals
                if (data.syncEvent == PlugAndHoleEvent.None)
                    if (data.tip1.normalsDirty || data.tip2.normalsDirty || data.plugBase.normalsDirty)
                        data.syncEvent = PlugAndHoleEvent.UpdateNormals;

                // process snap
                if (HoleSnapConstraint.ShouldSnap(data, plugBodyX, holeBodyX))
                    data.syncEvent = PlugAndHoleEvent.CreateSnap;
                else if (HoleSnapConstraint.ShouldUnsnap(data, plugBodyX, holeBodyX))
                    data.syncEvent = PlugAndHoleEvent.DestroySnap;
            }
        }

        public void ExecuteEvents()
        {
            ref var data = ref EntityStore.GetComponentData<PlugAndHoleData>(connectionEntity);

            if (data.syncEvent == PlugAndHoleEvent.None)
                return; // quick terminate

            if (data.syncEvent != PlugAndHoleEvent.DestroyGuides && !data.engaged)
            {
                //create guides
                data.engaged = true;
                var plugBodyX = World.main.GetTransformPosition(data.plugBody);
                var holeBodyX = World.main.GetTransformPosition(data.holeBody);

                pin1tip.Engage(ref data.tip1, plugBodyX, holeBodyX, Body, activeHole.body);
                pin2tip.Engage(ref data.tip2, plugBodyX, holeBodyX, Body, activeHole.body);
                plugBase.Engage(ref data.plugBase, plugBodyX, holeBodyX, Body, activeHole.body);

                onEngageGuides?.Invoke();
            }

            if (data.syncEvent == PlugAndHoleEvent.DestroyGuides && data.engaged)
            {
                //destroy guides
                data.engaged = false;
                pin1tip.Disengage(ref data.tip1);
                pin2tip.Disengage(ref data.tip2);
                plugBase.Disengage(ref data.plugBase);
                snap.Disengage(ref data);

                onDisengageGuides?.Invoke();
            }

            if (data.syncEvent == PlugAndHoleEvent.CreateSnap && !data.snapped)
            {
                //create snap joint
                snap.Engage(ref data, Body, activeHole.body);
                snapped = true;
                onPlugIn?.Invoke(activeHole);
            }

            if (data.syncEvent == PlugAndHoleEvent.DestroySnap && data.snapped)
            {
                //destroy snap joint
                snap.Disengage(ref data);
                snapped = false;
                onPlugOut?.Invoke(activeHole);
            }

            if (data.syncEvent != PlugAndHoleEvent.DestroyGuides)
            {
                //update guides
                pin1tip.WriteNormals(ref data.tip1);
                pin2tip.WriteNormals(ref data.tip2);
                plugBase.WriteNormals(ref data.plugBase);
            }
        }

        void OnDrawGizmos()
        {
            float size = 0.5f;
            var t = GetRigidWorldTransform();
            float3 direction = math.mul(t.rot, Vector3.forward * size);
            Color color = Color.cyan;
            Matrix4x4 matrix;

            if (alignTransform != null)
            {
                matrix = Matrix4x4.TRS(alignTransform.position, alignTransform.rotation, alignTransform.localScale);
            }
            else
            {
                matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            }

            PlugAndHoleGizmos.DrawGizmo(holeType, matrix, direction, color, size);
        }
    }
}