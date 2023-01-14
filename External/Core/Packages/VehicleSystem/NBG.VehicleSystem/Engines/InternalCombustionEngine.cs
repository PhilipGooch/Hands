using NBG.Core;
using NBG.Net;
using Recoil;
using UnityEngine;

namespace NBG.VehicleSystem
{
    /// <summary>
    /// main guideline:
    /// https://x-engineer.org/power-vs-torque/
    /// https://x-engineer.org/vehicle-acceleration-maximum-speed-modeling-simulation/
    /// rpm calculations:
    /// https://x-engineer.org/calculate-wheel-vehicle-speed-engine-speed/
    /// torque calculations:
    /// https://x-engineer.org/calculate-wheel-torque-engine/
    /// </summary>
    [DisallowMultipleComponent]
    public class InternalCombustionEngine : MonoBehaviour, IEngine, IManagedBehaviour, IOnFixedUpdate, INetBehavior
    {
        [SerializeField]
        InternalCombustionEngineSettings _engineSettings;

        public IEngineAttachment Attachment { get; private set; }

        float _accelerator;
        float _engineRPM;
        float _engineTorqueNm;
        float _transmissionTorqueNm;
        float _wheelRPM;
        int _currentGear;

        internal float DebugEngineRPM => _engineRPM;
        internal float DebugEngineTorqueNm => _engineTorqueNm;
        internal float DebugTransmissionTorqueNm => _transmissionTorqueNm;
        internal float DebugWheelRPM => _wheelRPM;

        public bool IsOn { get; set; } = true;

        public int Gear
        {
            get => _currentGear;
            set
            {
                _currentGear = Mathf.Clamp(value, -1, _engineSettings.forwardGearRatios.Length);
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
            Attachment = GetComponent<IEngineAttachment>();
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
            if (Attachment != null)
            {
                var wheelDirection = Mathf.Sign(Gear);
                var speedRad = wheelDirection * _wheelRPM * Mathf.PI / 30;
                var torqueNm = _transmissionTorqueNm;
                Attachment.TransmitDriveShaftPower(speedRad, torqueNm);
            }
        }

        void UpdateDriveTrain()
        {
            if (!IsOn)
            {
                _engineRPM = 0;
                _engineTorqueNm = 0;
                _transmissionTorqueNm = 0;
                _wheelRPM = 0;
                return;
            }

            var minRPM = _engineSettings.RPMToTorqueCurveAtFullLoad.keys[0].time;
            var maxRPM = _engineSettings.RPMToTorqueCurveAtFullLoad.keys[_engineSettings.RPMToTorqueCurveAtFullLoad.length - 1].time;
            _engineRPM = Mathf.Lerp(minRPM, maxRPM, _accelerator);

            _engineTorqueNm = _engineSettings.RPMToTorqueCurveAtFullLoad.Evaluate(_engineRPM);

            var gearRatio = Mathf.Abs(GetGearRatio(Gear));

            //Multiplying by Accelerator is not based on real world model.
            //In reality we start to drive even without accelerator pedal, just by releasing cluch, but
            //in games we expect vehicle go just if we are pressing forward
            _transmissionTorqueNm = _engineTorqueNm * gearRatio * _engineSettings.finalDriveRatio * _accelerator;

            if (gearRatio == 0)
            {
                _wheelRPM = 0;
            }
            else
            {
                //Multiplying by Accelerator is not based on real world model.
                //In reality we start to drive even without accelerator pedal, just by releasing cluch, but
                //in games we expect vehicle go just if we are pressing forward
                _wheelRPM = _engineRPM / gearRatio / _engineSettings.finalDriveRatio * _accelerator;
            }
        }

        public float GetGearRatio(int gear)
        {
            if (gear < -1 || gear > _engineSettings.forwardGearRatios.Length)
                throw new System.ArgumentOutOfRangeException();

            if (gear == -1)
                return _engineSettings.reverseGearRatio;
            else if (gear == 0)
                return 0.0f;
            else
                return _engineSettings.forwardGearRatios[gear - 1];
        }

        void OnValidate()
        {
            Debug.Assert(_engineSettings != null, $"EngineSettings is not set for vehicles {gameObject.name}", gameObject);
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
