using NBG.Core;
using NBG.Net;
using Recoil;
using UnityEngine;

namespace NBG.VehicleSystem
{
    /// <summary>
    /// This is simplified engine then just maximum output is needed.
    /// Both wheel speed and wheel torque outpus is always maximum (multiplied by input)
    /// </summary>
    [RequireComponent(typeof(IChassis))]
    public class ConstantPowerEngine : MonoBehaviour, IEngine, IManagedBehaviour, IOnFixedUpdate, INetBehavior
    {
        [SerializeField]
        private float maximumPower;
        [SerializeField]
        private float reverseGearRatio;

        [SerializeField]
        [Tooltip("This is needed for wheel radius." +
            "Needed to calculate maximum wheel RPMs as we dont convert engine RPM to wheel RPM in simplified engine." +
            "Think of it as RPM multiplier")]
        private float smallestWheelRadius;

        IChassis chassis;

        float _accelerator;
        float _engineTorqueNm;
        float _transmissionTorqueNm;
        int _currentGear;

        internal float DebugCurrentSpeedMps => (chassis != null) ? chassis.CurrentSpeed : 0;
        internal float DebugCurrentSpeedKph => (chassis != null) ? chassis.CurrentSpeed * 3.6f : 0;
        internal float DebugEngineTorqueNm => _engineTorqueNm;
        internal float DebugTransmissionTorqueNm => _transmissionTorqueNm;

        public bool IsOn { get; set; } = true;

        public int Gear
        {
            get => _currentGear;
            set
            {
                _currentGear = Mathf.Clamp(value, -1, 1);
            }
        }

        public float Accelerator
        {
            get => _accelerator;
            set
            {
                if (value < 0.0f || value > 1.0f)
                    throw new System.ArgumentOutOfRangeException();
                _accelerator = value;
            }
        }

        void IManagedBehaviour.OnLevelLoaded()
        {
            chassis = GetComponent<IChassis>();
            Debug.Assert(chassis != null, $"{nameof(InternalCombustionEngine)} expects an {nameof(IChassis)} component.", gameObject);
            OnFixedUpdateSystem.Register(this);
        }

        void IManagedBehaviour.OnAfterLevelLoaded()
        {
        }

        void IManagedBehaviour.OnLevelUnloaded()
        {
            OnFixedUpdateSystem.Unregister(this);
        }

        bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

        void IOnFixedUpdate.OnFixedUpdate()
        {
            UpdateDriveTrain();
            var wheelDirection = Mathf.Sign(Gear);
            var neededRotationsPerSecond = chassis.TargetSpeed / (2 * Mathf.PI * smallestWheelRadius);
            var neededRadPerSecond = neededRotationsPerSecond * 360 * Mathf.Deg2Rad;
            var speedRad = wheelDirection * Accelerator * neededRadPerSecond;
            var torqueNm = _transmissionTorqueNm;

            chassis.TransmitDriveShaftPower(speedRad, torqueNm);
        }

        void UpdateDriveTrain()
        {
            if (!IsOn)
            {
                _engineTorqueNm = 0;
                _transmissionTorqueNm = 0;
                return;
            }

            _engineTorqueNm = maximumPower;

            var gearRatio = Mathf.Abs(GetGearRatio(Gear));

            _transmissionTorqueNm = _engineTorqueNm * gearRatio * _accelerator;
        }

        float GetGearRatio(int gear)
        {
            if (gear < -1 || gear > 1)
                throw new System.ArgumentOutOfRangeException();

            if (gear == -1)
                return reverseGearRatio;
            else if (gear == 0)
                return 0.0f;
            else
                return 1;
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