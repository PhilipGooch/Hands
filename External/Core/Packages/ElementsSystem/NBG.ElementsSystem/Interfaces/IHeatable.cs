using System;

namespace NBG.ElementsSystem
{
    /// <summary>
    /// Interface for all elementsSystem objects which can heat/lose heat
    /// </summary>
    public interface IHeatable
    {
        public float HeatThreshold { get; }
        public float TimeToHeat { get; }
        public float TimeToCoolDown { get; }
        public bool CanBeCooledByExtinguishers { get; }
        public float ExtinguisherMultiplier { get; }

        /// <summary>
        /// [0-1]
        /// </summary>
        public float HeatAmount { get; }


        /// <summary>
        /// On HeatThreshold Reached while heating
        /// </summary>
        public event ElementsSystemEventWithEffectsHandler OnHeated;

        /// <summary>
        /// On HeatThreshold Reached while cooling
        /// </summary>
        public event ElementsSystemEventWithEffectsHandler OnCooledDown;

        /// <summary>
        /// To override HeatAmount manualy
        /// </summary>
        /// <param name="heatAmount"></param>
        void SetHeatChanged(float heatAmount);
        /// <summary>
        /// Every time HeatAmount changes this callbacks is triggered
        /// </summary>
        public event Action<float> OnHeatAmountChanged;
    }
}
