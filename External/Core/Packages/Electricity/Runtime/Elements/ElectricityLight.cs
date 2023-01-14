using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Electricity
{
    /// <summary>
    /// A specific kind of reciever that modifies the intensity of a light based on the power input.
    /// </summary>
    public class ElectricityLight : ElectricityReceiver
    {
        [SerializeField] private float maxIntensity;
        [SerializeField] private Light lightBulb;
        private void Update()
        {
            lightBulb.intensity = maxIntensity * Mathf.Clamp01((currentAmps - minAmps) / (maxAmps - minAmps));
        }
    }
}
