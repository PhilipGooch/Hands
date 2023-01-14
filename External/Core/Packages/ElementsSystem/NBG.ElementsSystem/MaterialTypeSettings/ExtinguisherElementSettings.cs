using UnityEngine;

namespace NBG.ElementsSystem
{
    /// <summary>
    /// Generic settings for extinguisher element
    /// </summary>
    [CreateAssetMenu(fileName = "ExtinguisherElementSettings", menuName = "[NBG] ElementsSystem/ExtinguisherElementSettings")]
    public class ExtinguisherElementSettings : ScriptableObject
    {
        [Header("WindRelatedSettings")]
        [SerializeField, Range(0f, 1)]
        float _extinguisherStrength = 1;
        public float ExtinguisherStrength => _extinguisherStrength;
    }
}
