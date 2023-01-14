using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using NBG.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;
using NBG.Core;
using NBG.Core.GameSystems;

[assembly: Preserve] // IL2CPP

namespace Recoil.Gravity
{
    /// <summary>
    /// A system that applies custom gravity to bodies using recoil. Gravity properties per object have 
    /// lazy initialization, so if an object uses default unity gravity, it won't contain an entry in the system
    /// until something requests that info or until gravity for that object is altered.
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsBeforeSolve))]
    public class GravitySystem : GameSystem
    {
        public static GravitySystem Instance
        {
            get
            {
                return instance;
            }
        }
        [ClearOnReload]
        private static GravitySystem instance;


        /// <summary>
        /// Stores the base and current gravities of bodies. Only items that have CustomGravity script register immediately.
        /// Items with default gravity settings get their base values stored when gravity is overwritten.
        /// Key - Recoil bodyId
        /// </summary>
        private Dictionary<int, BodyGravitySettings> perItemGravitySettings = new Dictionary<int, BodyGravitySettings>();
        private List<int> deadBodyCache = new List<int>();

        private UnityEngine.Profiling.CustomSampler applyGravitySampler;

        public GravitySystem()
        {
            instance = this;
            applyGravitySampler = UnityEngine.Profiling.CustomSampler.Create("Custom gravity");

            WritesData(typeof(Recoil.WorldJobData));
        }

        private void EnsureMainGravitySettingsPresent(int bodyId)
        {
            if (!perItemGravitySettings.ContainsKey(bodyId))
            {
                Rigidbody body = ManagedWorld.main.GetRigidbody(bodyId);
                if (body == null)
                {
                    Debug.LogError($"EnsureMainGravitySettingsPresent failed to get a valid Rigidbody for bodyId {bodyId}");
                    return;
                }

                // Item did not exist, so we look only at rigidbody settings for default gravity
                GravityState mainGravityState = new GravityState()
                {
                    gravityType = body.useGravity ? GravityType.GlobalDefault : GravityType.None,
                    customGravity = float3.zero,
                };

                perItemGravitySettings.Add(bodyId, new BodyGravitySettings(mainGravityState, mainGravityState, true));
            }
        }

        private void SyncGravityStateWithRigidbody(int bodyId)
        {
            var useGravity = perItemGravitySettings[bodyId].CurrentGravity.gravityType == GravityType.GlobalDefault;

            Rigidbody rigidBody = ManagedWorld.main.GetRigidbody(bodyId);
            if (rigidBody != null)
                rigidBody.useGravity = useGravity;

            Recoil.World.main.ConfigureBodySleep(bodyId, allowed: useGravity);
        }

        public float3 GetGravity(int bodyId)
        {
            EnsureMainGravitySettingsPresent(bodyId);

            GravityState objectGravitySettings = perItemGravitySettings[bodyId].CurrentGravity;

            switch (objectGravitySettings.gravityType)
            {
                case GravityType.None:
                    return float3.zero;
                case GravityType.GlobalDefault:
                    return Recoil.World.main.gravity;
                case GravityType.Custom:
                    return objectGravitySettings.customGravity;
                default:
                    return float3.zero;
            }
        }

        public void SetMainGravityAllowOverride(int bodyId, bool mainGravityCanBeOverriden)
        {
            EnsureMainGravitySettingsPresent(bodyId);
            perItemGravitySettings[bodyId].mainCanBeOverriden = mainGravityCanBeOverriden;
            SyncGravityStateWithRigidbody(bodyId);
        }

        public void SetMainGravity(int bodyId, GravityState mainGravityState, bool mainGravityCanBeOverriden=true)
        {
            DEBUG_CheckGravityValid(mainGravityState, bodyId);

            if (!perItemGravitySettings.ContainsKey(bodyId))
            {
                perItemGravitySettings.Add(bodyId, new BodyGravitySettings(mainGravityState, mainGravityState, mainGravityCanBeOverriden));
            }
            else
            {
                perItemGravitySettings[bodyId].mainCanBeOverriden = mainGravityCanBeOverriden;
                perItemGravitySettings[bodyId].mainGravity = mainGravityState;
            }

            SyncGravityStateWithRigidbody(bodyId);
        }

        public void SetModifiedOverrideGravity(int bodyId, GravityState overrideGravityState)
        {
            EnsureMainGravitySettingsPresent(bodyId);

            DEBUG_CheckGravityValid(overrideGravityState, bodyId);

            perItemGravitySettings[bodyId].AddOverrideGravityModify(overrideGravityState);
        }

        public void SetGravityOverride (int bodyId, int overrideGravityId, GravityType overrideType, float3 customOverrideGravity=default, bool suppressDoubleOverrideWarning=false)
        {
            EnsureMainGravitySettingsPresent(bodyId);
            BodyGravitySettings targetSettings = perItemGravitySettings[bodyId];

            if (!suppressDoubleOverrideWarning && targetSettings.overrideExists)
            {
                Debug.LogWarning($"[{nameof(GravitySystem)}] Overriding an override. Only main gravity will be stored, " +
                    $"override will not revert back to previous override");
            }

            GravityState targetState = new GravityState(overrideGravityId, overrideType, customOverrideGravity);
            DEBUG_CheckGravityValid(targetState, bodyId);

            targetSettings.overrideExists = true;
            perItemGravitySettings[bodyId].overrideGravity = targetState;
            SyncGravityStateWithRigidbody(bodyId);
        }

        public void ClearGravityOverride(int bodyId)
        {
            if (perItemGravitySettings.TryGetValue(bodyId, out BodyGravitySettings settings))
            {
                settings.overrideExists = false;
                SyncGravityStateWithRigidbody(bodyId);
            }
        }

        protected override void OnUpdate()
        {
            applyGravitySampler.Begin();

            // NOTE: Tried with parallel jobs. But since the size of affected bodies varies we need temp arrays and copying of data.
            // Jobs are on average faster by 0.01ms but generate a little garbage. Rolled back to non-jobs.
            foreach (KeyValuePair<int, BodyGravitySettings> bodyGravity in perItemGravitySettings)
            {
                int bodyId = bodyGravity.Key;
                Body body = Recoil.World.main.GetBody(bodyId);

                if (!body.alive)
                {
                    deadBodyCache.Add(bodyId);
                    continue;
                }

                GravityState currentGravity = bodyGravity.Value.CurrentGravity;
                if (currentGravity.gravityType == GravityType.Custom)
                {
                    Rigidbody rb = ManagedWorld.main.GetRigidbody(bodyId);
                    if (rb.isKinematic || !rb.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    float3 timeScaledGravity = currentGravity.customGravity * Recoil.World.main.dt;
                    Recoil.World.main.AddLinearVelocity(bodyId, timeScaledGravity);
                }
            }

            for (int i=0; i<deadBodyCache.Count; i++)
            {
                perItemGravitySettings.Remove(deadBodyCache[i]);
            }
            deadBodyCache.Clear();

            applyGravitySampler.End();
        }

        private const float kGravityLowerBound = 0.005f;
        private const float kGravityUpperBound = 50;

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DEBUG_CheckGravityValid (GravityState state, int bodyId)
        {
            if (state.gravityType == GravityType.Custom)
            {
                float3 gravitySquared = state.customGravity * state.customGravity;
                float gravityMagnitude = math.sqrt(gravitySquared.x + gravitySquared.y + gravitySquared.z);
                GameObject offender = ManagedWorld.main.GetRigidbody(bodyId).gameObject;

                if (gravityMagnitude < kGravityLowerBound)
                {
                    Debug.LogError($"Applying custom gravity of a very small magnitue {gravityMagnitude} to {offender.name}. " +
                        $"Disable gravity instead to make use of native unity physics sleep", offender);
                }
                if (gravityMagnitude > kGravityUpperBound)
                {
                    Debug.LogError($"Applying custom gravity of a very large magnitue {gravityMagnitude} to {offender.name}", offender);
                }
            }
        }
    }
}

