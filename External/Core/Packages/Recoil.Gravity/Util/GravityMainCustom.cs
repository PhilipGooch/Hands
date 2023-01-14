using NBG.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil.Gravity
{
    /// <summary>
    /// This is used to set a custom main gravity for an object. Main gravity is the gravity that is used when
    /// no overrides are being applied. You can also disallow any override for this object.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GravityMainCustom : MonoBehaviour, IManagedBehaviour
    {
        [Tooltip("Can the main gravity (custom or otherwise) be overriden?")]
        [SerializeField]
        private bool allowGravityOverride = true;
        [SerializeField]
        [Tooltip("Does it use a custom value for its main gravity")]
        private bool useCustomGravity = true;
        [SerializeField]
        [Tooltip("The custom main gravity value")]
        private float3 customGravity = new float3(0f, -1.6f, 0f);

        private int bodyId = -1;

        public bool AllowGravityOverride
        {
            get
            {
                return allowGravityOverride;
            }
            set
            {
                allowGravityOverride = value;
                GravitySystem.Instance.SetMainGravityAllowOverride(bodyId, allowGravityOverride);
            }
        }

        void IManagedBehaviour.OnLevelLoaded() { }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            bodyId = ManagedWorld.main.FindBody(GetComponent<Rigidbody>());

            if (useCustomGravity)
            {
                GravitySystem.Instance.SetMainGravity(bodyId, new GravityState(-1, GravityType.Custom, customGravity), allowGravityOverride);
            }
            else
            {
                GravitySystem.Instance.SetMainGravityAllowOverride(bodyId, allowGravityOverride);
            }
        }

        void IManagedBehaviour.OnLevelUnloaded() { }        
    }
}
