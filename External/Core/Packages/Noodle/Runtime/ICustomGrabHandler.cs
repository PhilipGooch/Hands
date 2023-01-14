using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public interface ICustomGrabHandler
    {
        void OnGrab(float3 position, out Rigidbody body);
        void OnRelease(Rigidbody body);
    }
}