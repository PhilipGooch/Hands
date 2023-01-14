using UnityEngine;
using System;
using Recoil;

namespace NBG.VehicleSystem
{
    public class PhysicalAxle : IPhysicalAxle
    {
        #region IAxle
        public void TransmitPower(float speedRads, float torqueNm)
        {
            switch (_settings.Differential)
            {
                case PhysicalAxisDifferentialMode.Open:
                    {
                        var torquePerHub = torqueNm / _hubs.Length;
                        foreach (var hub in _hubs)
                        {
                            hub.TransmitPower(speedRads, torquePerHub);
                        }
                    }
                    break;

                default:
                    throw new NotImplementedException($"{nameof(PhysicalAxle)} does not support transmitting power in differential mode '{_settings.Differential}'.");
            }
        }

        public float CurrentSpeedRads
        {
            get
            {
                var speed = 0.0f;
                foreach (var hub in _hubs)
                {
                    speed += hub.CurrentSpeedRads;
                }
                return speed / _hubs.Length;
            }
        }

        public void TransmitSteering(float steeringValue)
        {
            switch (_hubs.Length)
            {
                case 0:
                    break;
                case 1:
                    if (Settings.IsSteerable)
                    {
                        float endValue = CalculateCurrentSteering(_axleForwardOffsetFromTurningCircle, _chassis.TurningRadius, _settings.HalfWidth, -steeringValue);
                        _hubs[0].TransmitSteering(endValue);
                    }
                    break;
                default:
                    if (Settings.IsSteerable)
                    {
                        float wheelHorizontalOffset = -_settings.HalfWidth;
                        float horizontalOffsetIncrement = (2 * _settings.HalfWidth) / (_hubs.Length - 1);
                        for (int i = 0; i < _hubs.Length; i++)
                        {
                            float endLValue = CalculateCurrentSteering(_axleForwardOffsetFromTurningCircle, _chassis.TurningRadius, wheelHorizontalOffset, -steeringValue);
                            _hubs[i].TransmitSteering(endLValue);
                            wheelHorizontalOffset += horizontalOffsetIncrement;
                        }
                    }
                    break;
            }
        }

        float CalculateCurrentSteering(
            float wheelBase, float turnRadius, float wheelHorizontalOffset,
            float inputValue)
        {
            if (inputValue < 0)
                return Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius - wheelHorizontalOffset)) * inputValue;
            else if (inputValue > 0)
                return Mathf.Rad2Deg * Mathf.Atan(wheelBase / (turnRadius + wheelHorizontalOffset)) * inputValue;
            return 0;
        }

        public void TransmitBrakes(float brakesValue)
        {
            foreach (var hub in _hubs)
            {
                if (Settings.CanBrake)
                    hub.TransmitBrakes(brakesValue);
            }
        }

        public ref readonly PhysicalAxleSettings Settings { get { return ref _settings; } }
        #endregion

        PhysicalAxleSettings _settings;
        PhysicalChassis _chassis;
        ReBody _chassisRebody;
        IPhysicalWheelHubAssembly[] _hubs;
        public IWheelHubAssembly[] Hubs => _hubs;
        
        float _axleForwardOffsetFromTurningCircle;

        public PhysicalAxle(PhysicalChassis chassis, PhysicalAxleSettings settings)
        {
            _settings = settings;
            _chassis = chassis;
            _chassisRebody = new ReBody(chassis.Rigidbody);

            int hubCount = settings.HubsWithWheels.Length;
            _hubs = new IPhysicalWheelHubAssembly[hubCount];
            for (int hubIndex = 0; hubIndex < hubCount; ++hubIndex)
            {
                var localPos = Vector3.zero;
                localPos.z += settings.ForwardOffset;
                localPos.y += settings.VerticalOffset;
                float hubPositionMultiplier;
                if (hubCount == 1)
                {
                    hubPositionMultiplier = 1;
                }
                else
                {
                    hubPositionMultiplier = Mathf.Lerp(-1, 1, hubIndex / (float)(settings.HubsWithWheels.Length - 1));
                }
                
                localPos.x += settings.HalfWidth * hubPositionMultiplier;
                var globalPos = chassis.transform.TransformPoint(localPos);
                var globalRot = chassis.transform.rotation * Quaternion.identity;

                _hubs[hubIndex] = new PhysicalWheelHubAssembly(chassis, settings, globalPos, globalRot, hubIndex);
            }

            _axleForwardOffsetFromTurningCircle = settings.ForwardOffset - chassis.TurningCenterForwardOffset;

            if (settings.IsSteerable)
            {
                Debug.Assert(chassis.TurningRadius > 0, "TurnRadius: needs to be possitive");
            }
        }
    }
}
