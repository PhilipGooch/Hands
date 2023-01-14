using NBG.Core;
using Recoil;
using UnityEngine;

public static class Servo
{
    public static float MoveStepper(float stepperPos, float actualPos, float targetPos, float maxVel)
    {
        var dt = Time.fixedDeltaTime;
        if ((targetPos - actualPos) * (stepperPos - actualPos) < 0) // switched direction, snap stepper to current position
            stepperPos = actualPos;
        else
            stepperPos = Mathf.MoveTowards(actualPos, stepperPos, maxVel * 2 * dt); // prevent moving too far away
        stepperPos = Mathf.MoveTowards(stepperPos, targetPos, maxVel * dt);
        return stepperPos;
    }

    public static Quaternion GetRelativeRotation(ReBody body, ReBody parentBody)
    {
        var parentRot = parentBody != null ? parentBody.rotation : Quaternion.identity;
        var localToParent = Quaternion.Inverse(parentRot) * body.rotation;
        return localToParent;
    }

    public static Quaternion GetRelativeRotation(Rigidbody body, Rigidbody parentBody)
    {
        return GetRelativeRotation(new ReBody(body), new ReBody(parentBody));
    }

    public static Quaternion GetInvRigRotation(ReBody body, Joint joint, float rigAngle)
    {
        var rigRotation = GetRelativeRotation(body, new ReBody(joint.connectedBody)) * Quaternion.AngleAxis(-rigAngle * Mathf.Rad2Deg, joint.axis);//.AngleAxisToQuaternion();
        return Quaternion.Inverse(rigRotation);
    }

    public static Quaternion GetInvRigRotation(Rigidbody body, Joint joint, float rigAngle)
    {
        return GetInvRigRotation(new ReBody(body), joint, rigAngle);
    }

    public static float ReadJointAngle(ReBody body, Joint joint, Quaternion invRigRotation, float prevPos)
    {
        var parent = new ReBody(joint.connectedBody);
        var axis = body.TransformDirection(joint.axis.normalized);
        var currentPos = Math3d.GetTwist(GetRelativeRotation(body, parent) * invRigRotation, axis);
        currentPos = Math2d.NormalizeAngle(currentPos - prevPos) + prevPos;
        return currentPos;
    }

    public static float ReadJointAngle(Rigidbody body, Joint joint, Quaternion invRigRotation, float prevPos)
    {
        return ReadJointAngle(new ReBody(body), joint, invRigRotation, prevPos);
    }

    public static void ApplyAngularDynamics(ReBody body, Joint joint, float targetPos, float currentPos, float maxA, float maxVel)
    {
        var dt = Time.fixedDeltaTime;

        var connectedRe = new ReBody(joint.connectedBody);

        var axis = body.TransformDirection(joint.axis.normalized);
        var currentVel = Vector3.Dot(axis, body.angularVelocity);
        if (connectedRe.BodyExists)
            currentVel -= Vector3.Dot(axis, connectedRe.angularVelocity);

        //// find angular acceleration due to gravity, knowing that axis is fixed
        var tensor = Dynamics.TensorAtPointAxis(body, body.worldCenterOfMass - body.TransformPoint(joint.anchor), axis);
        // axis point gives acceleration 
        var force = Physics.gravity * body.mass;
        var torque = Vector3.Cross(body.worldCenterOfMass - body.TransformPoint(joint.anchor), force);
        var angularAfterGravity = Vector3.Dot(torque, axis) / tensor;
        var externalA = angularAfterGravity;

        var a = Dynamics.ConstantDeceleration(currentPos - targetPos, currentVel, 0, 0, maxA, maxVel, dt) - externalA;

        if (Mathf.Abs(a) > .001f)
        {
            var f = axis * a * tensor;
            body.AddTorqueAtPosition(f, body.TransformPoint(joint.anchor));
            if (connectedRe.BodyExists)
                connectedRe.AddTorqueAtPosition(-f, connectedRe.TransformPoint(joint.connectedAnchor));
        }
    }

    public static void ApplyAngularDynamics(Rigidbody body, Joint joint, float targetPos, float currentPos, float maxA, float maxVel)
    {
        ApplyAngularDynamics(new ReBody(body), joint, targetPos, currentPos, maxA, maxVel);
    }

    public static void ApplyLinearDynamics(Rigidbody body, Joint joint, float targetPos, float currentPos, float maxA, float maxVel)
    {
        ApplyLinearDynamics(new ReBody(body), joint, targetPos, currentPos, maxA, maxVel);
    }

    public static void ApplyLinearDynamics(ReBody body, Joint joint, float targetPos, float currentPos, float maxA, float maxVel)
    {
        var dt = Time.fixedDeltaTime;
        var axis = body.TransformDirection(joint.axis.normalized);
        var currentVel = Vector3.Dot(axis, body.velocity);
        var connectedBody = new ReBody(joint.connectedBody);
        if (connectedBody.BodyExists)
            currentVel -= Vector3.Dot(axis, connectedBody.velocity);

        var externalA = Vector3.Dot(axis, Physics.gravity);
        var a = Dynamics.ConstantDeceleration(currentPos - targetPos, currentVel, 0, 0, maxA, maxVel, dt) - externalA;

        if (Mathf.Abs(a) > .001f)
        {
            var force = axis * a * body.mass;
            body.AddForceAtPosition(force, body.TransformPoint(joint.anchor));
            if (connectedBody.BodyExists)
                connectedBody.AddForceAtPosition(-force, connectedBody.TransformPoint(joint.connectedAnchor));

        }
    }
}
