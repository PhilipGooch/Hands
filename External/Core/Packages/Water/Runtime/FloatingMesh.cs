using NBG.Core;
using NBG.Core.GameSystems;
using NBG.LogicGraph;
using Recoil;
using UnityEngine;

namespace NBG.Water
{
    /// <summary>
    /// Something that floats.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloatingMesh : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate, IFloatingMeshSettings
    {
        [SerializeField]
        FloatingMeshInstance instance = new FloatingMeshInstance();
        internal FloatingMeshInstance Instance => instance;

        [SerializeField]
        FloatingMeshSimulationData settings = FloatingMeshSimulationData.CreateDefault(); // Networking: setting this on client will do nothing. Changes on server are not transmitted to client.
        ref FloatingMeshSimulationData IFloatingMeshSettings.SimulationData => ref settings;

        bool _lastFrameSubmerged;

        /// <summary>
        /// Checks if object is submerged.
        /// Networking: works on server and on client based on local object positions. //TODO: implement networking.
        /// </summary>
        [NodeAPI("Is submerged?", scope: NodeAPIScope.Generic)]
        public bool Submerged => instance.Submerged;

        public delegate void OnWaterContactChangeDelegate(bool isSubmerged);
        /// <summary>
        /// Fires when object gets submerged or dry.
        /// Networking: fires on server and on client based on local object positions. //TODO: figure out how to do Sim/View for networking.
        /// </summary>
        [NodeAPI("On water contact change", scope: NodeAPIScope.Sim)]
        public event OnWaterContactChangeDelegate OnWaterContactChange;

        [NodeAPI("Buoyancy multiplier", scope: NodeAPIScope.Sim)]
        public float BuoyancyMultiplier { get => ((IFloatingMesh)instance).InstanceData.buoyancyMultiplier; set => ((IFloatingMesh)instance).InstanceData.buoyancyMultiplier = value; } // Networking: setting this on client will do nothing. Changes on server are not transmitted to client.

        void IManagedBehaviour.OnLevelLoaded()
        {
            instance.Initialize(gameObject, this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);

            instance.Shutdown();
        }

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;
        void IOnFixedUpdate.OnFixedUpdate()
        {
            var submerged = instance.Submerged;
            if (submerged != _lastFrameSubmerged)
            {
                OnWaterContactChange?.Invoke(submerged);
                _lastFrameSubmerged = submerged;
            }
        }

#if UNITY_EDITOR
        void Update()
        {
            if (!UnityEditor.Selection.Contains(this.gameObject))
                return;

            var waterSystem = GameSystemWorldDefault.Instance?.GetExistingSystem<WaterSystem>();
            waterSystem?.DrawDebugGizmos(instance);
        }
#endif
    }
}
