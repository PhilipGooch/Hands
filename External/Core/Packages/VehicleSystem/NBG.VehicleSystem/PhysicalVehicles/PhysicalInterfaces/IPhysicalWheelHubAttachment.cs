using UnityEngine;

namespace NBG.VehicleSystem
{
    public interface IPhysicalWheelHubAttachment : IWheelHubAttachment
    {
        float Radius { get; }
        Rigidbody Rigidbody { get; }

        void OnAttach(IPhysicalChassis chassis, IPhysicalWheelHubAssembly hub);
        void OnDetach();
    }
}
