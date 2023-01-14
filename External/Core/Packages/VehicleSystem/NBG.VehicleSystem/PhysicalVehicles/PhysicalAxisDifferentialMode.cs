namespace NBG.VehicleSystem
{
    public enum PhysicalAxisDifferentialMode
    {
        /// <summary>
        /// Torque is not transmitted.
        /// </summary>
        NotPowered,

        /// <summary>
        /// Same torque applied to each wheel, even if they rotate at different speeds.
        /// </summary>
        Open,
    }
}
