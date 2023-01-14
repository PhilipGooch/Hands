using NBG.Core;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil.Gravity
{
    /// <summary>
    /// This is used to create areas/volumes that apply override gravity to all objects inside them.
    /// It contains the main volume where the override will be applied and can contain a list of smaller volumes
    /// that carve out areas inside the main volume, where no override will be applied.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GravityOverrideRegion : MonoBehaviour, IManagedBehaviour
    {
        /// <summary>
        /// All gravity override regions present in level. Invalid in IManagedBehavior.OnAwake. Valid in IManagedBehaviour.OnStart.
        /// </summary>
        [ClearOnReload(true)]
        public static List<GravityOverrideRegion> gravityOverrideRegionsInLevel { get; private set; } = new List<GravityOverrideRegion>();

        [Range(1, 100)]
        [SerializeField] private int overrideGravityId = 1;
        [SerializeField] float3 gravityOverride = new float3(0f, -3f, 0f);

        [SerializeField] private BodyDetector[] defaultGravitySubAreas;

        /// <summary>
        /// Parameters: (int bodyId)
        /// </summary>
        public event Action<int> OnOverrideGravitySet;

        /// <summary>
        /// Parameters: (int bodyId)
        /// </summary>
        public event Action<int> OnOverrideGravityClear;

        public float3 GravityOverride { get { return gravityOverride; } }

        private struct ObjectGravityAreaStatus
        {
            public bool inOverrideGravityArea;
            public int inDefaultGravitySubAreasRefCount;

            public ObjectGravityAreaStatus(bool inOverrideGravityArea, int inDefaultGravitySubAreasRefCount)
            {
                this.inOverrideGravityArea = inOverrideGravityArea;
                this.inDefaultGravitySubAreasRefCount = inDefaultGravitySubAreasRefCount;
            }
        }

        private BodyIdTriggerProximityTracker mainAreaDetector = new BodyIdTriggerProximityTracker();
        private Dictionary<int, ObjectGravityAreaStatus> affectedBodies = new Dictionary<int, ObjectGravityAreaStatus>();

        private Collider myCollider = null;
        private Collider MyCollider
        {
            get
            {
                if (myCollider == null)
                    myCollider = GetComponent<Collider>();
                return myCollider;
            }
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            gravityOverrideRegionsInLevel.Add(this);

            for (int i=0; i<defaultGravitySubAreas.Length; i++)
            {
                defaultGravitySubAreas[i].OnRigidBodyEnter += OnEnterDefaultGravitySubArea;
                defaultGravitySubAreas[i].OnRigidBodyLeave += OnLeaveDefaultGravitySubArea;
            }

            Collider _ = MyCollider; // Pre-warm
        }

        void IManagedBehaviour.OnAfterLevelLoaded() { }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            gravityOverrideRegionsInLevel.Remove(this);

            for (int i = 0; i < defaultGravitySubAreas.Length; i++)
            {
                if (defaultGravitySubAreas[i] != null)
                {
                    defaultGravitySubAreas[i].OnRigidBodyEnter -= OnEnterDefaultGravitySubArea;
                    defaultGravitySubAreas[i].OnRigidBodyLeave -= OnLeaveDefaultGravitySubArea;
                }
            }

            foreach (KeyValuePair<int, ObjectGravityAreaStatus> affectedBody in affectedBodies)
            {
                ObjectGravityAreaStatus objectStatus = affectedBody.Value;
                if (objectStatus.inOverrideGravityArea && objectStatus.inDefaultGravitySubAreasRefCount == 0)
                {
                    GravitySystem.Instance.ClearGravityOverride(affectedBody.Key);
                }
            }
            affectedBodies.Clear();
        }

        /// <summary>
        /// If no status present in records, creates a default and updates the records 
        /// </summary>
        private ObjectGravityAreaStatus EnsureExistsAndGetObjectGravityAreaStatus(int bodyId)
        {
            ObjectGravityAreaStatus areaStatus;
            if (!affectedBodies.TryGetValue(bodyId, out areaStatus))
            {
                areaStatus = new ObjectGravityAreaStatus(false, 0);
                affectedBodies.Add(bodyId, areaStatus);
            }

            return areaStatus;
        }

        private void SetGravityOverridenObjectProperties (int bodyId)
        {
            GravitySystem.Instance.SetGravityOverride(bodyId, overrideGravityId, GravityType.Custom, gravityOverride);

            OnOverrideGravitySet?.Invoke(bodyId);
        }

        private void ClearGravityOverridenObjectProperties (int bodyId)
        {
            GravitySystem.Instance.ClearGravityOverride(bodyId);

            OnOverrideGravityClear?.Invoke(bodyId);
        }

        private void OnEnterDefaultGravitySubArea (int bodyId)
        {
            ObjectGravityAreaStatus areaStatus = EnsureExistsAndGetObjectGravityAreaStatus(bodyId);

            areaStatus.inDefaultGravitySubAreasRefCount++;
            affectedBodies[bodyId] = areaStatus;

            if (areaStatus.inDefaultGravitySubAreasRefCount == 1 && areaStatus.inOverrideGravityArea)
            {
                ClearGravityOverridenObjectProperties(bodyId);
            }
        }

        private void OnLeaveDefaultGravitySubArea(int bodyId)
        {
            ObjectGravityAreaStatus areaStatus = EnsureExistsAndGetObjectGravityAreaStatus(bodyId);

            areaStatus.inDefaultGravitySubAreasRefCount--;
            affectedBodies[bodyId] = areaStatus;

            if (areaStatus.inDefaultGravitySubAreasRefCount < 0)
            {
                Rigidbody rigidbody = ManagedWorld.main.GetRigidbody(bodyId);
                Debug.LogError($"Gravity default sub-area reference counter below zero: {areaStatus.inDefaultGravitySubAreasRefCount}." +
                    $" {rigidbody.name} exited more sub-areas than it entered.");
                areaStatus.inDefaultGravitySubAreasRefCount = 0;
            }

            if (areaStatus.inDefaultGravitySubAreasRefCount == 0)
            {
                if (areaStatus.inOverrideGravityArea)
                {
                    SetGravityOverridenObjectProperties(bodyId);
                }
                else
                {
                    affectedBodies.Remove(bodyId); // It's neither in main override area nor in default sub-areas. Remove.
                }
            }
        }

        private void OnBodyEnterOverrideGravityArea(int bodyId)
        {
            ObjectGravityAreaStatus areaStatus = EnsureExistsAndGetObjectGravityAreaStatus(bodyId);

            areaStatus.inOverrideGravityArea = true;
            affectedBodies[bodyId] = areaStatus;

            if (areaStatus.inDefaultGravitySubAreasRefCount == 0)
            {
                SetGravityOverridenObjectProperties(bodyId);
            }
        }

        private void OnBodyLeaveOverrideGravityArea(int bodyId)
        {
            ObjectGravityAreaStatus areaStatus = EnsureExistsAndGetObjectGravityAreaStatus(bodyId);

            areaStatus.inOverrideGravityArea = false;
            affectedBodies[bodyId] = areaStatus;

            if (areaStatus.inDefaultGravitySubAreasRefCount == 0)
            {
                ClearOverrideAndRemoveBodyTracking(bodyId); // It's neither in main override area nor in default sub-areas. Remove.
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            int bodyId = mainAreaDetector.OnTriggerEnter(other);
            
            if (bodyId < 0)
                return;

            OnBodyEnterOverrideGravityArea(bodyId);
        }

        private void OnTriggerExit(Collider other)
        {
            int bodyId = mainAreaDetector.OnTriggerLeave(other);

            if (bodyId < 0)
                return;

            OnBodyLeaveOverrideGravityArea(bodyId);
        }

        public void ClearOverrideAndRemoveBodyTracking (int bodyId)
        {
            if (affectedBodies.ContainsKey(bodyId))
            {
                ClearGravityOverridenObjectProperties(bodyId);
                affectedBodies.Remove(bodyId);
                mainAreaDetector.ForceRemoveEntry(bodyId);
                for (int i=0; i < defaultGravitySubAreas.Length; i++)
                {
                    defaultGravitySubAreas[i].ForceRemoveEntry(bodyId);
                }
            }
        }

        public void SetGravity(float3 newGravity)
        {
            gravityOverride = newGravity;

            foreach (KeyValuePair<int, ObjectGravityAreaStatus> affectedBody in affectedBodies)
            {
                ObjectGravityAreaStatus objectStatus = affectedBody.Value;
                if (objectStatus.inOverrideGravityArea && objectStatus.inDefaultGravitySubAreasRefCount == 0)
                {
                    GravitySystem.Instance.SetGravityOverride(affectedBody.Key, overrideGravityId, GravityType.Custom, gravityOverride, true);
                }
            }
        }

        public bool WouldSphereBeAffectedByGravityOverride(Vector3 centerWorldSpace, float radius)
        {
            const float kEpsilon = 1e-5f;
            if (radius < kEpsilon)
            {
                Debug.LogWarning($"Radius very small. Possibility of rounding errors, " +
                    $"please use at least {kEpsilon} even if you want to check only whether a point is affected by gravity override.");
                radius = kEpsilon;
            }

            for (int i = 0; i < defaultGravitySubAreas.Length; i++)
            {
                if (defaultGravitySubAreas[i].IsNearCollider(centerWorldSpace, radius))
                {
                    return false;
                }
            }

            Vector3 closestPoint = MyCollider.ClosestPoint(centerWorldSpace);
            return (closestPoint - centerWorldSpace).sqrMagnitude <= (radius * radius);
        }
    }
}
