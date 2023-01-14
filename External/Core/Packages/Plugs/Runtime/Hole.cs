using NBG.Core;
using NBG.LogicGraph;
using Recoil;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Plugs
{
    /// <summary>
    /// Socket controller
    /// </summary>
    [System.Serializable]
    public class Hole : MonoBehaviour, IManagedBehaviour
    {
        TriggerProximityList<Plug> nearbyPlugs = new TriggerProximityList<Plug>(ProximityListComponentSearch.InRigidbodyHierarchy);
        [SerializeField]
        HoleType holeType;
        public HoleType HoleType => holeType;

        [Tooltip("Allows overriding the center and orientation of the hole. Snapping works based on the distance from the center of the hole.")]
        [SerializeField] Transform alignTransform;

        const float kMinDisengageDistance = 0.00001f;
        //.3 was the default value (since it was configured for 0.5 size radius)
        [Tooltip("Distance between the pivot of the socket and the plug after which guide joints are destroyed (or created if lower)")]
        [SerializeField]
        float disengageDistance = 0.3f;
        internal float DisengageDistance => disengageDistance;

        [SerializeField]
        float engageStartMaxAngle = 45;
        internal float EngageStartMaxAngle => engageStartMaxAngle;
        [Tooltip("At what distance plug is considered as fully socketed")]
        [SerializeField]
        float plugDist = .02f;
        internal float PlugDist => plugDist;
        [Tooltip("At what distance plug is considered as no longer fully socketed")]
        [SerializeField]
        float unplugDist = .1f;
        internal float UnplugDist => unplugDist;

        [SerializeField]
        bool preventSnap = false;
        internal bool PreventSnap => preventSnap;

        [SerializeField]
        [Tooltip("Start guiding the plugs into the hole earlier.")]
        bool engageDeepGuides;
        internal bool EngageDeepGuides => engageDeepGuides;

        public event Action onEngageGuides;
        public event Action onDisengageGuides;
        [NodeAPI("OnPlugIn")]
        public event Action onPlugIn;
        [NodeAPI("OnPlugOut")]
        public event Action onPlugOut;

        bool snapped;
        [NodeAPI("IsPluggedIn")]
        public bool IsPluggedIn => snapped;

        Plug activePlug;
        public Plug ActivePlug => activePlug;

        public Rigidbody body { get; private set; }
        public int bodyId { get; private set; }

        #region IManagedBehaviour

        void IManagedBehaviour.OnLevelLoaded()
        {
            body = GetComponent<Rigidbody>();
            Debug.Assert(body != null, $"{nameof(Hole)} found without RigidBody!", this);
            bodyId = ManagedWorld.main.FindBody(body);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            foreach (var plug in nearbyPlugs)
                if (plug != null && plug.ActiveHole == this)
                    plug.Disconnect(this);
            nearbyPlugs.ClearItemsAndProximityData();
        }

        #endregion

        #region UnityEvents

        void OnValidate()
        {
            if (disengageDistance == 0)
                disengageDistance = kMinDisengageDistance;
        }

        private void OnTriggerEnter(Collider other)
        {
            var plug = nearbyPlugs.OnTriggerEnter(other);
            if (CanConnect(plug))
            {
                plug.Connect(this);
                onEngageGuides?.Invoke();
                plug.onPlugIn += PlugPluggedIn;
                plug.onPlugOut += PlugPluggedOut;
                activePlug = plug;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var plug = nearbyPlugs.OnTriggerLeave(other);
            if (CanDisconnect(plug))
            {
                plug.Disconnect(this);
                plug.onPlugIn -= PlugPluggedIn;
                plug.onPlugOut -= PlugPluggedOut;
                onDisengageGuides?.Invoke();
                activePlug = null;
            }
        }

        #endregion

        void PlugPluggedIn(Hole hole)
        {
            if (hole == this)
            {
                snapped = true;
                onPlugIn?.Invoke();
            }
        }

        void PlugPluggedOut(Hole hole)
        {
            if (hole == this)
            {
                snapped = false;
                onPlugOut?.Invoke();
            }
        }

        public virtual bool CanConnect(Plug plug)
        {
            return activePlug == null && plug != null && plug.ActiveHole == null && plug.HoleType == HoleType;
        }

        public virtual bool CanDisconnect(Plug plug)
        {
            return plug != null && activePlug == plug && plug.ActiveHole == this && plug.HoleType == HoleType;
        }

        public RigidTransform GetRigidWorldTransform()
        {
            return (alignTransform != null ? alignTransform : transform).GetRigidWorldTransform();
        }

        void OnDrawGizmos()
        {
            float size = 0.5f;
            var t = GetRigidWorldTransform();
            float3 direction = math.mul(t.rot, Vector3.forward * size);
            Color color = Color.yellow;
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