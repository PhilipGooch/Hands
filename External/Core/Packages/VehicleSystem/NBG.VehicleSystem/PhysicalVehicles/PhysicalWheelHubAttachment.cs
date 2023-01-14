using NBG.Core;
using Recoil;
using UnityEngine;

namespace NBG.VehicleSystem
{
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalWheelHubAttachment : MonoBehaviour, IPhysicalWheelHubAttachment
    {
        [SerializeField]
        GameObject _visualsOnlyGO;

        [SerializeField]
        float _radius;
        public float Radius => _radius;

        Rigidbody _rigidbody;
        public Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody>();
                return _rigidbody;
            }
        }

        //We update visuals on Update instead of fixedUpdate, and we don't need rigidbodies for that as well
        private void LateUpdate()
        {
            if (_chassis != null && _hub != null && _visualsOnlyGO != null)
            {
                _visualsOnlyGO.transform.position = _localPositionOffset + _hub.HubTransform.position;
                _visualsOnlyGO.transform.localEulerAngles = _localRotationOffset + new Vector3(_hub.CurrentXAngle, _hub.CurrentYAngle, 0);
            }
        }

        IPhysicalWheelHubAssembly _hub;
        IPhysicalChassis _chassis;
        Transform _oldParent;
        Vector3 _oldPosition;
        Vector3 _oldRotation;
        Vector3 _localPositionOffset;
        Vector3 _localRotationOffset;
        void IPhysicalWheelHubAttachment.OnAttach(IPhysicalChassis chassis, IPhysicalWheelHubAssembly hub)
        {
            if (_visualsOnlyGO == null)
            {
                return;
            }

            _chassis = chassis;
            _hub = hub;
            _oldParent = _visualsOnlyGO.transform.parent;
            _oldPosition = _visualsOnlyGO.transform.localPosition;
            _oldRotation = _visualsOnlyGO.transform.localEulerAngles;
            _localPositionOffset = _visualsOnlyGO.transform.localPosition;
            _localRotationOffset = _visualsOnlyGO.transform.localEulerAngles;
            _visualsOnlyGO.transform.SetParent(chassis.Rigidbody.transform);
        }

        void IPhysicalWheelHubAttachment.OnDetach()
        {
            if (_visualsOnlyGO == null)
            {
                return;
            }

            _visualsOnlyGO.transform.SetParent(_oldParent);
            _oldParent = null;
            _visualsOnlyGO.transform.localPosition = _oldPosition;
            _visualsOnlyGO.transform.localEulerAngles = _oldRotation;
            _hub = null;
            _chassis = null;
            _localPositionOffset = Vector3.zero;
            _localRotationOffset = Vector3.zero;
        }

        void OnValidate()
        {
            if (_visualsOnlyGO != null)
            {
                Debug.Assert(_visualsOnlyGO.GetComponentInChildren<Collider>() == null, "Wheel Visuals cant have collider, vehicle will not move correctly then", _visualsOnlyGO);
            }
        }
    }
}
