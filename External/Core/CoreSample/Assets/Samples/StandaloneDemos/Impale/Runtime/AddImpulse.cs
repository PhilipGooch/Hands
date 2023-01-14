using NBG.Core;
using Recoil;
using UnityEngine;

namespace CoreSample.ImpaleDemo
{
    public class AddImpulse : MonoBehaviour, IManagedBehaviour
    {
        [HideInInspector]
        [SerializeField]
        new private Rigidbody rigidbody;
        private ReBody reBody;
        [SerializeField]
        Vector3 torque;
        [SerializeField]
        float force;
        [SerializeField]
        Vector3 axis = Vector3.forward;

        private void OnValidate()
        {
            if (rigidbody == null)
                rigidbody = GetComponent<Rigidbody>();
        }

        [ContextMenu("Add Impulse and Torque")]
        void AddImpulseAndTorque()
        {
            reBody.AddTorque(torque, ForceMode.Impulse);
            var dir = transform.TransformDirection(axis);
            reBody.rigidbody.AddForce(dir.normalized * force, ForceMode.Impulse);
        }

        public void OnLevelLoaded()
        {
            reBody = new ReBody(rigidbody);
            AddImpulseAndTorque();
        }

        public void OnAfterLevelLoaded()
        {
        }

        public void OnLevelUnloaded()
        {
        }
    }
}
