namespace NBG.VehicleSystem
{
    public interface IChassis : IEngineAttachment
    {
        float MaxSpeed { get; }
        float TargetSpeed { get; set; }
        float CurrentSpeed { get; }

        IAxle[] Axles { get; }

        float TurningCenterForwardOffset { get; }
        float TurningRadius { get; }

        /// <summary>
        /// Actual speed of the drive shaft.
        /// Return same value as last received via TransmitDriveShaftPower to simulate a perfect clutch.
        /// Calculate average of powered axels otherwise.
        /// </summary>
        float CurrentDriveShaftSpeedRads { get; }

        /// <summary>
        /// Braking signal applied to the chassis
        /// </summary>
        float BrakingState { get; set; }

        /// <summary>
        /// Steering signal applied to the chassis
        /// </summary>
        float SteeringState { get; set; }
    }
}