using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.VehicleSystem
{
    [CreateAssetMenu(fileName = "SuspensionSettings", menuName = "[NBG] VehicleSystem/SuspensionSettings", order = 1)]
    public class SuspensionSettingsScriptableObject : ScriptableObject
    {
        /// <summary>
        /// 0 - disabled
        /// </summary>
        public float SuspensionLength;
        public float SpringForce;
        public float SpringDampening;
    }
}