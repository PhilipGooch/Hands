using NBG.Core;
using NBG.Net;
using Recoil;
using UnityEngine;

namespace NBG.Recoil.Net
{
    /// <summary>
    /// Overrides default values for network serialisation.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NetRigidbodySettingsOverride : MonoBehaviour, IManagedBehaviour
    {
        [SerializeField] NetPrecisionSettings settings = NetPrecisionSettings.DEFAULT;
        [SerializeField] Rigidbody relativeToTransform;
        
        public NetPrecisionSettings Settings => settings;
        public Rigidbody RelativeTo => relativeToTransform;
        public int RelativeToBodyId { get; private set; } = World.environmentId;

        void IManagedBehaviour.OnLevelLoaded()
        {
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            if (relativeToTransform != null)
            {
                RelativeToBodyId = ManagedWorld.main.FindBody(relativeToTransform, true);
            }
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
        }
    }
}
