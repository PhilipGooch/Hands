using System;

namespace NBG.ElementsSystem
{
    /// <summary>
    /// Interface for all elementsSystem objects which can burn
    /// </summary>
    public interface IFlammable
    {
        public float BurnTime { get; }
        public float TimeToIgnite { get; }
        public float TimeBeforeSelfExtinguish { get; }
        public float TimeBeforeSelfIgnite { get; }
        public float TimeForFireIncrease { get; }
        public bool CanBeIgnitedByHeatable { get; }
        public float HeatableIgniteThreshold { get; }
        public bool CanBeExtinguishedByExtinguishers { get; }
        public float MinimumExtinguisherStrengthNeeded { get; }


        /// <summary>
        /// How much is it burning [0;1]. value 0.1 is burning too! Even smallest value is treated as maximum temperature
        /// </summary>
        public float FlameAmount { get; }
        /// <summary>
        /// Is it burning right now
        /// </summary>
        public bool IsBurning { get; }
        /// <summary>
        /// It fully burned down. Can't be ignited again
        /// </summary>
        public bool IsBurnedOut { get; }


        public event ElementsSystemEventWithEffectsHandler OnExtinguished;
        public event ElementsSystemEventWithEffectsHandler OnIgnited;
        /// <summary>
        /// Triggered every time then BurningAmount is changed
        /// </summary>
        public event Action<float> OnBurningAmountChanged;
        public event ElementsSystemEventWithEffectsHandler OnBurnedOut;


        public void Ignite(bool withEffects);
        public void Extinguish(bool withEffects);

        /// <summary>
        /// This will not ignite, not extinguish flammable object. If it is ignited, then burning will change and then start increasing again, if not burning - does nothing.
        /// </summary>
        /// <param name="flameAmount"></param>
        public void SetFlameAmount(float flameAmount);
    }
}
