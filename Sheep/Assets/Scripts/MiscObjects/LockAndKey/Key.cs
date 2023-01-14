using Plugs;
using UnityEngine;

public class Key : Plug, IGrabNotifications
{
    [SerializeField]
    ConfigurableJoint joint;
    [SerializeField]
    Rigidbody anchor;

    float originalDrag;

    void OnValidate()
    {
        if(joint == null)
            joint = GetComponent<ConfigurableJoint>();
    }

    public void LockBlade(Transform center, bool isAcceptedKey, Rigidbody attachTo = null)
    {
        originalDrag = Body.angularDrag;

        transform.rotation = center.rotation;

        Body.useGravity = false;
        Body.angularDrag = 100;

        if (attachTo != null)
            joint.connectedBody = attachTo;
        else
            joint.connectedBody = anchor;

        joint.anchor = Vector3.zero;

        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        if (!isAcceptedKey)
            joint.angularXMotion = ConfigurableJointMotion.Locked;

        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
    }

    public void UnlockBlade()
    {
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;

        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;

        joint.connectedBody = null;
        Body.useGravity = true;
        Body.angularDrag = originalDrag;
    }

    public void OnGrab(Hand hand, bool firstGrab) { }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
            ForceDestroySnapAndGuides();
    }
}
