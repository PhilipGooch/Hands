using UnityEngine;

namespace NBG.Water
{
    public enum FloatingMeshMode
    {
        Normal,
        NormalizeMass,
    }

    [System.Serializable]
    public struct FloatingMeshSimulationData
    {
        [Tooltip("Vector for the direction to ignore when appplying force to the hull")]
        public Vector3 ignoreHydrodynamicForce;

        [Tooltip("The pressure in a particular direction")]
        public float pressureLinear;

        [Tooltip("The square of the pressure")]
        public float pressureSquare;

        [Tooltip("The amount of suction in a particular direction ")]
        public float suctionLinear;

        [Tooltip("The square of the suction")]
        public float suctionSquare;

        [Tooltip("How fast the power should fall off over the hull")]
        public float falloffPower;

        //[Tooltip("How much to be effected by the wind (currently not used)")]
        //public float bendWind;

        public static FloatingMeshSimulationData CreateDefault()
        {
            return new FloatingMeshSimulationData
            {
                pressureLinear = 20,
                pressureSquare = 10,
                suctionLinear = 20,
                suctionSquare = 10,
                falloffPower = 1,
                //bendWind = 0.1f
            };
        }
    }

    [System.Serializable]
    public struct FloatingMeshInstanceData
    {
        public FloatingMeshMode mode;

        [Tooltip("Adjust bouyancy according to normalized mass value (kg).")]
        public float buoyancyNormalizedMass;

        [Tooltip("Final bouyancy multiplier.")]
        public float buoyancyMultiplier;

        public static FloatingMeshInstanceData CreateDefault()
        {
            return new FloatingMeshInstanceData
            {
                mode = FloatingMeshMode.Normal,
                buoyancyNormalizedMass = 1,
                buoyancyMultiplier = 1,
            };
        }
    }
}
