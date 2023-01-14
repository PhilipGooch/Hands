namespace NBG.VehicleSystem
{
    public interface IAxle
    {
        IWheelHubAssembly[] Hubs { get; }

        void TransmitPower(float speedRads, float torqueNm);
        /// <summary>
        /// Actual speed of the axle shaft.
        /// Calculate average of WHAs, or implement a differential.
        /// </summary>
        float CurrentSpeedRads { get; }

        void TransmitSteering(float steeringValue);
        void TransmitBrakes(float brakesValue);
    }
}
