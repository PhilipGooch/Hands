using UnityEngine;

namespace Recoil
{
    /// <summary>
    /// Applies custom Rigidbody settings before registering it with Recoil
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodySettingsOverride : MonoBehaviour
    {
        [SerializeField]
        RigidbodySettings settings = new RigidbodySettings();

        public RigidbodySettings Settings => settings;

        public static void Apply(Rigidbody rigidbody, RigidbodySettings settings)
        {
            if (settings.centerOfMassOverride != null)
            {
                rigidbody.centerOfMass = rigidbody.transform.InverseTransformPoint(settings.centerOfMassOverride.position);
            }
        }
    }
}
