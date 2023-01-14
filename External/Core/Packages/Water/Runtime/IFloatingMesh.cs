using UnityEngine;

namespace NBG.Water
{
    public interface IFloatingMeshSettings
    {
        public ref FloatingMeshSimulationData SimulationData { get; }
    }

    public interface IFloatingMesh
    {
        public Rigidbody Rigidbody { get; }
        public GameObject FloatingGameObject { get; }
        public GameObject HullGameObject { get; }
        public IWaterSensor HullWaterSensor { get; }
        public Mesh HullMesh { get; }

        public ref FloatingMeshInstanceData InstanceData { get; }
    }
}
