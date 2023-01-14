using UnityEngine;

namespace NBG.ElementsSystem
{
    /// <summary>
    /// Generic settings for heatable element
    /// </summary>
    [CreateAssetMenu(fileName = "HeatableElementSettings", menuName = "[NBG] ElementsSetings/HeatableElementSettings")]
    public class HeatableElementSettings : ScriptableObject
    {
        [SerializeField, Range(0f, 1)]
        [Tooltip("After HeatAmount will reach this threshold it will be counted as Heated")]
        float _heatThreshold = 1;
        public float HeatThreshold => _heatThreshold;

        [SerializeField, Range(0f, float.MaxValue)]
        [Tooltip("How long is needed to heat to achieve from 0 to 1 of heat amount")]
        float _timeToHeat = 1;
        public float TimeToHeat => _timeToHeat;

        [SerializeField, Range(0f, float.MaxValue)]
        [Tooltip("How long is needed to pass without heating to cooldown from 1 to 0 of heat amount")]
        float _timeToCoolDown = 0;
        public float TimeToCoolDown => _timeToCoolDown;

        [SerializeField]
        bool _canBeHeatedByFlammables = true;
        public bool CanBeHeatedByFlammables => _canBeHeatedByFlammables;

        [SerializeField]
        bool _canBeHeatedByHeatables = true;
        public bool CanBeHeatedByHeatables => _canBeHeatedByHeatables; 
        
        [SerializeField, Range(1, 10)]
        float _heatAbsorptionResistance = 2;
        public float HeatAbsorptionResistance => _heatAbsorptionResistance;

        [Header("HeatableRelatedSettings")]
        [SerializeField]
        bool _canBeCooledByExtinguishers;
        public bool CanBeCooledByExtinguishers => _canBeCooledByExtinguishers;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("if 0 - cooling duration will be TimeToCoolDown, if 1 then it will be multiplied by full extinguisher strength")]
        float _extinguisherMultiplier;
        public float ExtinguisherMultiplier => _extinguisherMultiplier;
    }
}
