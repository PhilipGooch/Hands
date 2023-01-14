using UnityEngine;

namespace NBG.VehicleSystem
{
    [CreateAssetMenu(fileName = "InternalCombustionEngineSettings", menuName = "[NBG] VehicleSystem/InternalCombustionEngineSettings")]
    public class InternalCombustionEngineSettings : ScriptableObject
    {
        // Defaults taken from https://en.wikipedia.org/wiki/Toyota_C_transmission#C251
        public AnimationCurve RPMToTorqueCurveAtFullLoad;
        public float reverseGearRatio = -3.250f;
        public float[] forwardGearRatios = new float[5] { 3.545f, 1.904f, 1.392f, 1.031f, 0.815f };
        public float finalDriveRatio = 4.312f;
    }
}