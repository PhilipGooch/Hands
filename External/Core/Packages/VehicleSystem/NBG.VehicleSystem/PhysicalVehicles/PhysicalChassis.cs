using NBG.Core;
using NBG.Net;
using Recoil;
using System.Linq;
using UnityEngine;

namespace NBG.VehicleSystem
{
    [DisallowMultipleComponent]
    public class PhysicalChassis : MonoBehaviour, IPhysicalChassis, IManagedBehaviour, IOnFixedUpdate, INetBehavior
    {
        [Header("Geometry")]
        [SerializeField, ReadOnlyInPlayModeField]
        bool useRearWheelsForTurningPivot;
        [SerializeField, ReadOnlyInPlayModeField]
        float turningCenterForwardOffset = 0.0f; // Forward (Z)
        [SerializeField, ReadOnlyInPlayModeField]
        float turningRadius = 10.0f; // Right (X)

        [Header("Other")]
        [SerializeField] float maxSpeed = 10.0f;
        [SerializeField, ReadOnlyInPlayModeField]
        GameObject _COM;

        [SerializeField, ReadOnlyInPlayModeField]
        PhysicalAxleSettings[] _axleSettings = new PhysicalAxleSettings[0];



        IPhysicalAxle[] _axles = new PhysicalAxle[0];
        public IAxle[] Axles => _axles;

        [SerializeField]
        Rigidbody _ownRigidbody;
        ReBody _ownRigidbodyReBody;

        #region IPhysicalChassis
        public Rigidbody Rigidbody => _ownRigidbody;
        #endregion

        #region IChassis
        public float MaxSpeed => maxSpeed;

        float targetSpeed;
        public float TargetSpeed { get => targetSpeed; set => targetSpeed = value; }

        public float CurrentSpeed
        {
            get
            {
                if (Application.isPlaying)
                {
                    return _ownRigidbodyReBody.velocity.magnitude;
                }
                return 0;
            }
        }

        public float TurningCenterForwardOffset { get => turningCenterForwardOffset; set => turningCenterForwardOffset = value; }
        public float TurningRadius { get => turningRadius; set => turningRadius= Mathf.Abs(value); }

        public float CurrentDriveShaftSpeedRads
        {
            get
            {
                var poweredAxleCount = _axleSettings.Count(x => x.IsPowered);
                if (poweredAxleCount > 0)
                {
                    // Averaging all IAxle.CurrentSpeedRads for a naive implementation.
                    float speed = 0.0f;
                    foreach (var axle in _axles)
                    {
                        if (axle.Settings.IsPowered)
                            speed += axle.CurrentSpeedRads;
                    }
                    return speed / poweredAxleCount;
                }
                else
                {
                    return _currentDriveShaftSpeedRads;
                }
            }
        }

        float brakingState;
        public float BrakingState
        {
            get => brakingState;
            set
            {
                if (value < 0.0f || value > 1.0f)
                    throw new System.ArgumentOutOfRangeException();

                brakingState = value;
            }
        }

        float steeringState;
        public float SteeringState
        {
            get => steeringState;
            set
            {
                if (value < -1.0f || value > 1.0f)
                    throw new System.ArgumentOutOfRangeException();

                steeringState = value;
            }
        }
        #endregion

        #region IEngineAttachment
        float _currentDriveShaftSpeedRads;
        float _currentDriveShaftTorqueNm;
        public void TransmitDriveShaftPower(float speedRads, float torqueNm)
        {
            if (torqueNm < 0.0f)
                throw new System.ArgumentOutOfRangeException();

            _currentDriveShaftSpeedRads = speedRads;
            _currentDriveShaftTorqueNm = torqueNm;
        }
        #endregion

        float GetNewGuideOffsetValue(float previous, float desired) // reduce micro serialization changes
        {
            if (Mathf.Abs(previous - desired) < 0.0001f)
                return previous;
            return desired;
        }

        void OnValidate()
        {
            Debug.Assert(_ownRigidbody != null, $"Main Rigidbody is not set on {gameObject.name}", gameObject);

            if (useRearWheelsForTurningPivot)
            {
                float minimumOffset = _axleSettings != null && _axleSettings.Length > 0 ? _axleSettings[0].ForwardOffset : 0;
                foreach (var axleSettings in _axleSettings)
                {
                    if (minimumOffset > axleSettings.ForwardOffset)
                    {
                        minimumOffset = axleSettings.ForwardOffset;
                    }
                }
                turningCenterForwardOffset = minimumOffset;
            }

            turningRadius = Mathf.Abs(turningRadius);

            for (int i = 0; i < _axleSettings.Length; ++i)
            {
                var axle = _axleSettings[i];
                PhysicalAxleSettings settings = axle;

                if (axle.Guide != null)
                {
                    var localDelta = transform.InverseTransformPoint(axle.Guide.position);
                    settings.ForwardOffset = GetNewGuideOffsetValue(settings.ForwardOffset, localDelta.z);
                    settings.VerticalOffset = GetNewGuideOffsetValue(settings.VerticalOffset, localDelta.y);
                    settings.HalfWidth = GetNewGuideOffsetValue(settings.HalfWidth, Mathf.Abs(localDelta.x));
                }
                else
                {
                    settings.HalfWidth = Mathf.Abs(settings.HalfWidth);
                }

                _axleSettings[i] = settings;

                Debug.Assert(settings.SuspensionSettings == null || (settings.SuspensionSettings != null && settings.SuspensionSettings.SuspensionLength > 0), $"If suspension is not needed, then make suspension settings null, not 0. Vehicle: {this.gameObject.name}.");

                if (axle.HubsWithWheels != null)
                {
                    for (int hubIndex = 0; hubIndex < axle.HubsWithWheels.Length; hubIndex++)
                    {
                        Debug.Assert(axle.HubsWithWheels[hubIndex].Hub != null, "Missing Hub");

                        ref readonly var wheel = ref axle.HubsWithWheels[hubIndex].Wheel;
                        if (wheel != null)
                        {
                            Debug.Assert(wheel.GetComponent<IPhysicalWheelHubAttachment>() != null, "Wheel can't be attached without IPhysicalWheelHubAttachment", wheel);
                        }
                    }
                }
            }
        }

        void Setup() 
        {
            Debug.Assert(_COM != null);
            _ownRigidbodyReBody = new ReBody(_ownRigidbody);
            _ownRigidbodyReBody.centerOfMass = Rigidbody.InverseTransformPoint(_COM.transform.position);

            TargetSpeed = MaxSpeed;

            _axles = new IPhysicalAxle[_axleSettings.Length];
            for (int axleIndex = 0; axleIndex < _axleSettings.Length; ++axleIndex)
            {
                ref readonly var settings = ref _axleSettings[axleIndex];
                _axles[axleIndex] = new PhysicalAxle(this, settings);
            }

            // Setup colliders
            var collidersToIgnore = GetComponentsInChildren<Collider>();
            for (int i = 0; i < collidersToIgnore.Length; i++)
                for (int j = 0; j < collidersToIgnore.Length; j++)
                    if (i != j)
                        Physics.IgnoreCollision(collidersToIgnore[j], collidersToIgnore[i], true);
        }

        #region Generated

#if UNITY_EDITOR
        bool IsRigidbodyHubRequired(int axleIndex)
        {
            if (axleIndex >= AxleCount)
                return false;

            ref readonly var settings = ref GetAxleSettings(axleIndex);
            if (settings.SuspensionSettings != null || settings.IsSteerable)
            {
                return true;
            }
            return false;
        }

        internal Rigidbody AddRigidbodyToGameObject(GameObject gameObject)
        {
            var rigidbody = gameObject.AddComponent<Rigidbody>();
            rigidbody.drag = 0;
            rigidbody.angularDrag = 0;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            return rigidbody;
        }

        internal void VerifyIntegrity()
        {
            //remove obsolete hubs rigidbodies
            for (var axleIndex = 0; axleIndex < AxleCount; axleIndex++)
            {
                ref readonly var settings = ref GetAxleSettings(axleIndex);
                if (settings.HubsWithWheels != null)
                {
                    for (var wheelIndex = 0; wheelIndex < settings.HubsWithWheels.Length; wheelIndex++)
                    {
                        if (!IsRigidbodyHubRequired(axleIndex))
                        {
                            var rigidbody = settings.HubsWithWheels[wheelIndex].Hub.GetComponent<Rigidbody>();
                            if (rigidbody != null)
                                DestroyImmediate(rigidbody);

                            //TODO: apply prefabs
                        }
                    }
                }
            }

            // Create missing rigidbodies on hubs
            for (var axleIndex = 0; axleIndex < AxleCount; axleIndex++)
            {
                ref readonly var settings = ref GetAxleSettings(axleIndex);
                if (IsRigidbodyHubRequired(axleIndex))
                {
                    if (settings.HubsWithWheels != null)
                    {
                        for (var hubIndex = 0; hubIndex < settings.HubsWithWheels.Length; hubIndex++)
                        {
                            var hub = settings.HubsWithWheels[hubIndex].Hub;
                            if (hub != null)
                            {
                                var hubRigidbody = hub.GetComponent<Rigidbody>();
                                if (hubRigidbody == null)
                                {
                                    hubRigidbody = AddRigidbodyToGameObject(hub);
                                }

                                //validate hub mass
                                if (hubRigidbody.mass != settings.AdditionalMassForHubs)
                                {
                                    hubRigidbody.mass = settings.AdditionalMassForHubs;
                                }
                            }
                        }
                    }
                }
            }
        }
#endif
        #endregion

#if UNITY_EDITOR
        internal int AxleCount => _axleSettings.Length;
        internal ref readonly PhysicalAxleSettings GetAxleSettings(int index) { return ref _axleSettings[index]; }
        internal void SetAxleSettings(int index, in PhysicalAxleSettings settings) { _axleSettings[index] = settings; }
#endif
        void IManagedBehaviour.OnLevelLoaded()
        {
            OnFixedUpdateSystem.Register(this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
            Setup();
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

        void IOnFixedUpdate.OnFixedUpdate()
        {
            var poweredAxleCount = _axleSettings.Count(x => x.IsPowered);
            var torquePerAxle = 0f;
            if (poweredAxleCount > 0)
            {
                torquePerAxle = _currentDriveShaftTorqueNm / poweredAxleCount;
            }
            for (int i = 0; i < _axles.Length; i++)
            {
                if (_axles[i].Settings.IsSteerable)
                {
                    _axles[i].TransmitSteering(steeringState);
                }
                if (_axles[i].Settings.IsPowered)
                    _axles[i].TransmitPower(_currentDriveShaftSpeedRads, torquePerAxle);
                if (_axles[i].Settings.CanBrake)
                    _axles[i].TransmitBrakes(brakingState);
            }
        }


        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Rigidbody != null ? Rigidbody.transform.localToWorldMatrix : transform.localToWorldMatrix;

            Gizmos.color = Color.yellow;
            var turningCenterPos = Vector3.zero;
            turningCenterPos.z += turningCenterForwardOffset;
            turningCenterPos.x += turningRadius;
            Gizmos.DrawWireSphere(turningCenterPos, 0.25f);

            foreach (var axle in _axleSettings)
            {
                Gizmos.color = Color.red;
         
                var pos = Vector3.zero;
                pos.z += axle.ForwardOffset;
                var xzPos = pos;
                pos.y += axle.VerticalOffset;
                Gizmos.DrawSphere(pos, 0.1f);
                Gizmos.DrawLine(pos, xzPos);

                var side = Vector3.right * axle.HalfWidth;
                Gizmos.DrawCube(pos + side, new Vector3(0.05f, 0.2f, 0.2f));
                Gizmos.DrawCube(pos - side, new Vector3(0.05f, 0.2f, 0.2f));
                Gizmos.DrawLine(pos - side, pos + side);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos + side, turningCenterPos);
                Gizmos.DrawLine(pos - side, turningCenterPos);
                var oppositeTurningCenterPos = turningCenterPos;
                oppositeTurningCenterPos.x *= -1;
                Gizmos.DrawLine(pos + side, oppositeTurningCenterPos);
                Gizmos.DrawLine(pos - side, oppositeTurningCenterPos);

                if (axle.SuspensionSettings != null)
                {
                    Gizmos.color = Color.blue;

                    var sPos = pos;
                    var sPosBottom = sPos - Vector3.up * axle.SuspensionSettings.SuspensionLength;
                    Gizmos.DrawCube(sPos + side, Vector3.one * 0.025f);
                    Gizmos.DrawCube(sPosBottom + side, Vector3.one * 0.025f);
                    Gizmos.DrawLine(sPos + side, sPosBottom + side);

                    Gizmos.DrawCube(sPos - side, Vector3.one * 0.025f);
                    Gizmos.DrawCube(sPosBottom - side, Vector3.one * 0.025f);
                    Gizmos.DrawLine(sPos - side, sPosBottom - side);
                }
            }

            if (_axleSettings.Length > 0)
            {
                Gizmos.color = Color.red;
                var minHO = _axleSettings.Min(x => x.ForwardOffset);
                var maxHO = _axleSettings.Max(x => x.ForwardOffset);
                Gizmos.DrawLine(Vector3.forward * minHO, Vector3.forward * maxHO);
            }
        }

        void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            switch (authority)
            {
                case NetworkAuthority.Server:
                    OnFixedUpdateSystem.Register(this);
                    break;
                case NetworkAuthority.Client:
                    OnFixedUpdateSystem.Unregister(this);
                    break;
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}
