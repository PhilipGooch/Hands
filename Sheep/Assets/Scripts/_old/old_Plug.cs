using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

public interface IConstraint
{
    void Apply(ref Vector3 targetPos, ref Quaternion targetRot, ref Vector6 targetVel);
}

public class old_Plug : MonoBehaviour, IConstraint //, IGrabNotifications
{
    Rigidbody body;
    public Transform socket;
    public Vector3 socketAxis = Vector3.forward;
    public Vector3 myAxis = Vector3.down;
    ConfigurableJoint joint;
    private void OnEnable()
    {
        body = GetComponent<Rigidbody>();
       
    }


    public void Apply(ref Vector3 targetPos, ref Quaternion targetRot, ref Vector6 targetVel)
    {
        var axis = socket.TransformDirection(socketAxis);
        //var myAxis = Vector3.down;

        var offset = targetPos - socket.position;
        var dist = Vector3.Dot(offset, axis);

        var targetPos2 = targetPos - targetRot * myAxis; // point 1m away from tip along axis

        // calculate both point target as projected to target axis
        var projection1 = Math3d.ProjectPointOnLine(socket.position, axis, targetPos);
        var projection2 = projection1 + axis;

        // calculate strength - tip is attracted sooner than tail
        var strength1 = Mathf.InverseLerp(.5f, .25f, (projection1- targetPos).magnitude) * Mathf.InverseLerp(.5f,.25f,dist);
        var strength2 = Mathf.InverseLerp(.5f, .25f, (projection1 - targetPos).magnitude) * Mathf.InverseLerp(.25f, 0, dist);

        // pull to target positions
        targetPos = Vector3.Lerp(targetPos, projection1, strength1);
        targetPos2 = Vector3.Lerp(targetPos2, projection2, strength2);

        // calculate rotation
        var swing = Quaternion.FromToRotation(-(targetRot * myAxis), (targetPos2 - targetPos).normalized); // swing rotation to align with calculated targets
        targetRot = swing * targetRot;

        // project velocites as well
        targetVel = new Vector6(
            Vector3.Lerp(targetVel.angular, Vector3.Project(targetVel.angular, axis), strength2), 
            Vector3.Lerp(targetVel.linear, Vector3.Project(targetVel.linear, axis), strength1));
               
    }
}
