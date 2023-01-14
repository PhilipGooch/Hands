using UnityEngine;

namespace NBG.ElementsSystem
{
    /// <summary>
    /// Generic settings for flammable element
    /// </summary>
    [CreateAssetMenu(fileName = "FlammableElementSettings", menuName = "[NBG] ElementsSystem/FlammableElementSettings")]
    public class FlammableElementSettings : ScriptableObject
    {
        [SerializeField, Range(0f, float.MaxValue)]
        [Tooltip("How long fire can last. After reaching this value it can no longer be ignited again")]
        float _burnTime = 0;
        public float BurnTime => _burnTime;

        [SerializeField, Range(0f, float.MaxValue)]
        [Tooltip("How long it needs to be heated to ignite")]
        float _timeToIgnite = 0;
        public float TimeToIgnite => _timeToIgnite;

        [SerializeField, Range(0f, float.MaxValue)]
        [Tooltip("It will extinguish itself after this time")]
        float _timeBeforeSelfExtinguish = 0;
        public float TimeBeforeSelfExtinguish => _timeBeforeSelfExtinguish;

        [SerializeField, Range(0f, float.MaxValue)]
        float _timeBeforeSelfIgnite = 0;
        public float TimeBeforeSelfIgnite => _timeBeforeSelfIgnite;

        [SerializeField, Range(0f, float.MaxValue)]
        [Tooltip("After it is ignited this time is used to fully ignite.")]
        float _timeForFireIncrease = 0;
        public float TimeForFireIncrease => _timeForFireIncrease;

        [SerializeField]
        bool _canBeIgnitedByFlammables = true;
        public bool CanBeIgnitedByFlammables => _canBeIgnitedByFlammables;

        [Header("HeatableRelatedSettings")]
        [SerializeField]
        bool _canBeIgnitedByHeatable;
        public bool CanBeIgnitedByHeatable => _canBeIgnitedByHeatable;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Used only with heatable ignition")]
        float _heatableIgniteThreshold;
        public float HeatableIgniteThreshold => _heatableIgniteThreshold;


        [Header("HeatableRelatedSettings")]
        [SerializeField]
        bool _canBeExtinguishedByExtinguishers;
        public bool CanBeExtinguishedByExtinguishers => _canBeExtinguishedByExtinguishers;

        [SerializeField, Range(0f, 1f)]
        float _minimumExtinguisherStrengthNeeded;
        public float MinimumExtinguisherStrengthNeeded => _minimumExtinguisherStrengthNeeded;
    }
}
