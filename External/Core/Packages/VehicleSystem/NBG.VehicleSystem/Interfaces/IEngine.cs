namespace NBG.VehicleSystem
{
    public interface IEngine
    {
        /// <summary>
        /// Can turn on or turn off engine.
        /// Powered-down engine doesn't output any power.
        /// </summary>
        bool IsOn { get; set; }

        /// <summary>
        /// State of the accelerator [0;1]
        /// </summary>
        float Accelerator { get; set; }

        /// <summary>
        /// -1 for reverse
        /// 0 for neutral
        /// 1..N for forward
        /// </summary>
        int Gear { get; set; }
    }
}
