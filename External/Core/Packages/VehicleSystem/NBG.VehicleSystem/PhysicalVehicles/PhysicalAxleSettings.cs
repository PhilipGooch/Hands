using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NBG.VehicleSystem
{
    [Serializable]
    public struct HubsAndWheels
    {
        public GameObject Hub;
        public GameObject Wheel;
    }

    [Serializable]
    public struct PhysicalAxleSettings
    {
        [Header("Geometry")]

        [Tooltip("Automatically configure geometry based on a reference Transform")]
        public Transform Guide;
        public float ForwardOffset; // Forward (Z)
        public float VerticalOffset; // Up (Y)
        public float HalfWidth; // Right (X)


        [Header("Hubs")]
        public HubsAndWheels[] HubsWithWheels;
        [FormerlySerializedAs("AdditionalMassForIntermediateRigidbody")]
        public float AdditionalMassForHubs;


        [Header("Steering")]
        public bool IsSteerable;



        [Header("Suspension")]
        public SuspensionSettingsScriptableObject SuspensionSettings;



        [Header("Brakes")]

        [Tooltip("Maximum brake torque [Nm]. Zero disables braking.")]
        public float MaxBrakeTorqueNm;
        public bool CanBrake => !Mathf.Approximately(MaxBrakeTorqueNm, 0.0f);



        [Header("Power")]
        //[Tooltip("Is this axle connected to the drive shaft?")]
        public PhysicalAxisDifferentialMode Differential;
        public bool IsPowered => (Differential != PhysicalAxisDifferentialMode.NotPowered);
    }
}
