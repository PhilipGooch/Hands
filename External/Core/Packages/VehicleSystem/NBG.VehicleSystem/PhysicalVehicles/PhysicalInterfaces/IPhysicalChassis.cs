using UnityEngine;

namespace NBG.VehicleSystem
{
    public interface IPhysicalChassis : IChassis
    {
        Rigidbody Rigidbody { get; }
    }
}
