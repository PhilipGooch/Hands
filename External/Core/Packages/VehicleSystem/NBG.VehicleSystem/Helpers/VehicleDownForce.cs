using NBG.Core;
using Recoil;
using UnityEngine;

namespace NBG.VehicleSystem
{
    /// <summary>
    /// Vehicle optional addon which adds constant force down while grounded
    /// Could be improved as it doesn't check each each wheel, but takes vehicles center point and shoots down.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleDownForce : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
    {
        [SerializeField]
        private float rayDistance = 2f;
        [SerializeField]
        private float downForce = 0;

        private new Rigidbody rigidbody;
        private ReBody recoildRigidbodyConverter;

        void IManagedBehaviour.OnLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            rigidbody = GetComponent<Rigidbody>();
            recoildRigidbodyConverter = new ReBody(rigidbody);
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        public bool Enabled => isActiveAndEnabled;

        void IOnFixedUpdate.OnFixedUpdate()
        {
            if (IsGrounded())
            {
                recoildRigidbodyConverter.AddForce(Vector3.down * downForce);
            }
        }

        //check if vehicle is grounded or not(by shooting a ray downwards)
        //TODO: make better down force as right now it doesn't acctualy track if it is grounded, but instead just shoots one ray
        private bool IsGrounded() 
        {
            if (Physics.Raycast(recoildRigidbodyConverter.position, -rigidbody.transform.up, out RaycastHit hit, rayDistance))
            {
                return true;
            }
            return false;
        }

        void OnDrawGizmosSelected()
        {
            if (recoildRigidbodyConverter != null && rigidbody != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(recoildRigidbodyConverter.position, recoildRigidbodyConverter.position + (-rigidbody.transform.up * rayDistance));
            }
        }
    }
}