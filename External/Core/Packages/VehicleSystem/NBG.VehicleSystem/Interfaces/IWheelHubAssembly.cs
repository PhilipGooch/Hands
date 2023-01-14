using UnityEngine;

namespace NBG.VehicleSystem
{
    public interface IWheelHubAssembly
    {
        IWheelHubAttachment Attachment { get; }

        void TransmitPower(float speedRads, float torqueNm);
        void TransmitBrakes(float brakesValue);
        void TransmitSteering(float steeringAngleDegrees);
        float CurrentSpeedRads { get; }
        float CurrentXAngle { get; }
        float CurrentYAngle { get; }
        /// <summary>
        /// [0-1], 0 - no force applied, 1 - maximum force
        /// </summary>
        float CurrentLoad { get; }
    }
}
