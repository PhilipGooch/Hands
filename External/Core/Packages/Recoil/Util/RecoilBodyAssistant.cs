using UnityEngine;
using NBG.Core;

namespace Recoil
{
    /// <summary>
    /// Handles Recoil body destruction automatically.
    /// Not to be added manually in scenes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    internal class RecoilBodyAssistant : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private int _id = World.environmentId;

        public Rigidbody Rigidbody => _rigidbody;
        public int Id => _id;

        bool IsRegistered => _id != World.environmentId;

        private void Start()
        {
            Register();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void OnEnable()
        {
            if (IsRegistered)
            {
                // This is for synchronizing inertia tensors and center of mass, since a disabled object has undefined values.
                // and if object is disabled when scene is started, the recoil cache receives these uncalculated values.
                ManagedWorld.main.ResyncPhysXBody(_rigidbody);

                ManagedWorld.main.SetBodyPlacementImmediate(Id, _rigidbody.GetRigidTransformAtCenterOfMass());
            }
        }

        public void Register(Rigidbody rigidbody = null)
        {
            if (IsRegistered)
                return;

            _rigidbody = rigidbody;

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();
            
            if (_rigidbody == null)
                throw new System.InvalidOperationException($"RecoilBody on a GameObject without a Rigidbody: {gameObject.GetFullPath()}");

            if (TryGetComponent(out RigidbodySettingsOverride rso))
                RigidbodySettingsOverride.Apply(_rigidbody, rso.Settings);

            Debug.Assert(rigidbody == null || GetComponent<Rigidbody>() == rigidbody);
            _id = ManagedWorld.main.RegisterBody(_rigidbody);
        }

        public void Unregister()
        {
            if (IsRegistered)
            {
                ManagedWorld.main.UnregisterBody(_id);
                _id = World.environmentId;
                _rigidbody = null;
            }
        }

#if UNITY_EDITOR
        internal Body _bodyDump;
        internal bool _physxIsSleeping;

        private void LateUpdate()
        {
            if (IsRegistered)
            {
                _bodyDump = World.main.GetBody(_id);
                _physxIsSleeping = _rigidbody.IsSleeping();
            }
        }
#endif
    }
}
