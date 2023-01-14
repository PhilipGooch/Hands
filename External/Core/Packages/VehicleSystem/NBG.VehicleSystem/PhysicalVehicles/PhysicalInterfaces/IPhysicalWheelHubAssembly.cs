using UnityEngine;

namespace NBG.VehicleSystem
{
    public interface IPhysicalWheelHubAssembly : IWheelHubAssembly
    {
        Transform HubTransform { get; }
        void Attach(IPhysicalWheelHubAttachment attachment);
        void Detach();
    }
}
