using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using Recoil;

public interface IProjectHandAnchor
{
    void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot);
}


public static class ProjectAnchorUtils
{
    public static void ProjectAngular(ReBody body, Vector3 center, Vector3 axis, ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel,
        ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        // This is probably incorrect
        var rotationDiff = Quaternion.Inverse(body.rotation) * vrRot;
        var projectedRotation = Vector3.Project(rotationDiff.QToAngleAxis(), axis).AngleAxisToQuaternion();
        vrRot = body.rotation * projectedRotation;
        // Previous implementation - also incorrect
        //vrRot = Vector3.Project(vrRot.QuaternionToAngleAxis(), axis).AngleAxisToQuaternion();

        // let's work in local space
        var localPos = body.InverseTransformPoint(vrPos);
        var localVel = Quaternion.Inverse(body.rotation) * vrVel;
        // project to anchor plane
        localPos = Vector3.ProjectOnPlane(localPos - anchorPos, axis) + anchorPos;
        localVel = Vector3.ProjectOnPlane(localVel, axis);
        // project on circle
        var projectOnAxis = Vector3.Project(localPos - center, axis) + center;
        localVel = Vector3.ProjectOnPlane(localVel, (localPos - projectOnAxis).normalized);
        localPos = (anchorPos - projectOnAxis).magnitude * (localPos - projectOnAxis).normalized + projectOnAxis;
        vrPos = body.TransformPoint(localPos);
        vrVel = body.rotation * localVel;
    }

    public static void ProjectAngular(Rigidbody body, Vector3 center, Vector3 axis, ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel,
        ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        ProjectAngular(new ReBody(body), center, axis, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
    }

    // Similar to ProjectAngular but constrained within hinge joint limits
    public static void ProjectHingeJoint(Rigidbody body, HingeJoint joint, ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        ProjectHingeJoint(new ReBody(body), joint, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
    }

    public static void ProjectHingeJoint(ReBody body, HingeJoint joint, ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        var axis = joint.axis.normalized;
        var center = joint.anchor;

        // This is probably incorrect
        var rotationDiff = Quaternion.Inverse(body.rotation) * vrRot;
        var projectedRotation = Vector3.Project(rotationDiff.QToAngleAxis(), axis).AngleAxisToQuaternion();
        vrRot = body.rotation * projectedRotation;

        // Previous implementation - also incorrect
        //vrRot = Vector3.Project(vrRot.QuaternionToAngleAxis(), axis).AngleAxisToQuaternion();

        // let's work in local space
        var localPos = body.InverseTransformPoint(vrPos);
        var localVel = Quaternion.Inverse(body.rotation) * vrVel;


        if (joint.useLimits)
        {
            var jointAngle = joint.angle;
            var angle = jointAngle + Vector3.SignedAngle(anchorPos, localPos, axis);
            if (angle > joint.limits.max)
            {
                var targetAngle = joint.limits.max - jointAngle;
                var angleAxis = Quaternion.AngleAxis(targetAngle, axis);
                localPos = (angleAxis * anchorPos).normalized * localPos.magnitude;
            }
            if (angle < joint.limits.min)
            {
                var targetAngle = joint.limits.min - jointAngle;
                var angleAxis = Quaternion.AngleAxis(targetAngle, axis);
                localPos = (angleAxis * anchorPos).normalized * localPos.magnitude;
            }
        }

        // project to anchor plane
        localPos = Vector3.ProjectOnPlane(localPos - anchorPos, axis) + anchorPos;
        localVel = Vector3.ProjectOnPlane(localVel, axis);

        // project on circle
        var projectOnAxis = Vector3.Project(localPos - center, axis) + center;
        localVel = Vector3.ProjectOnPlane(localVel, (localPos - projectOnAxis).normalized);
        localPos = (anchorPos - projectOnAxis).magnitude * (localPos - projectOnAxis).normalized + projectOnAxis;
        vrPos = body.TransformPoint(localPos);
        vrVel = body.rotation * localVel;
    }

    public static void ProjectLinear(Rigidbody body, Vector3 center, Vector3 axis, ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        ProjectLinear(new ReBody(body), center, axis, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
    }

    public static void ProjectLinear(ReBody body, Vector3 center, Vector3 axis, ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        vrRot = body.rotation * anchorRot;
        vrAngular = Vector3.zero;

        var worldAnchor = body.TransformPoint(anchorPos);
        vrPos = Vector3.Project(vrPos - worldAnchor, body.TransformDirection(axis)) + worldAnchor;
        vrVel = Vector3.Project(vrVel, body.TransformDirection(Vector3.forward));
    }

}
