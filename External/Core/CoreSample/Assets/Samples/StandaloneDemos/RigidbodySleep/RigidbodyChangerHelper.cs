using NBG.Core;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class RigidbodyChangerHelper : MonoBehaviour, IManagedBehaviour, IOnFixedUpdate
{
    public Vector3 positionAdd;
    public Vector3 velocityAdd;
    public bool viaIOnFixedUpdateRecoil;

    int bodyId = World.environmentId;

    bool IOnFixedUpdate.Enabled => isActiveAndEnabled;

    public void OnLevelLoaded()
    {
        bodyId = ManagedWorld.main.FindBody(GetComponent<Rigidbody>());
    }

    public void OnFixedUpdate()
    {
        if (!viaIOnFixedUpdateRecoil)
            return;

        if (positionAdd != Vector3.zero)
        {
            var pos = World.main.GetBodyPosition(bodyId);
            pos.pos += new float3(positionAdd);
            World.main.SetBodyPosition(bodyId, pos);
        }

        if (velocityAdd != Vector3.zero)
        {
            var vel = World.main.GetVelocity(bodyId);
            vel.linear += new float3(velocityAdd);
            World.main.SetVelocity(bodyId, vel);
        }
    }

    public void OnLevelUnloaded()
    {
        
    }

    public void OnAfterLevelLoaded()
    {
        
    }

    void FixedUpdate()
    {
        if (viaIOnFixedUpdateRecoil)
            return;

        if (positionAdd != Vector3.zero)
        {
            GetComponent<Rigidbody>().position += positionAdd;
        }

        if (velocityAdd != Vector3.zero)
        {
            GetComponent<Rigidbody>().velocity += velocityAdd;
        }
    }
}
