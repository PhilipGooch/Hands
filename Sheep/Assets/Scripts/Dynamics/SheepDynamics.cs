#define SOFTCLAMP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using Recoil;

public static class Dynamics
{
    const float offAxisMultiplier = 2;
    const float softClampFalloff = .25f;

    public static Vector3 InvInertiaTensor(ReBody body)
    {
        return new Vector3(body.inertiaTensor.x > 0 ? 1 / body.inertiaTensor.x : 0,
            body.inertiaTensor.y > 0 ? 1 / body.inertiaTensor.y : 0,
            body.inertiaTensor.z > 0 ? 1 / body.inertiaTensor.z : 0);
    }

    public static Vector3 InvInertiaTensor(Rigidbody body)
    {
        return InvInertiaTensor(new ReBody(body));
    }

    // calculates effective mass for linear force and given point
    public static float InvMassAtPointAxis(ReBody body, Vector3 relPoint, Vector3 axis)
    {
        var invMass = body.invMass;
        var invInertiaDiagLocal = InvInertiaTensor(body);
        var angularAxis = Vector3.Cross(relPoint, axis);
        var m_aJ = Quaternion.Inverse(body.rotation * body.inertiaTensorRotation) * angularAxis;
        var invAngularMass = Vector3.Dot(Vector3.Scale(invInertiaDiagLocal, m_aJ), m_aJ);
        return invMass + invAngularMass;
    }

    public static float InvMassAtPointAxis(Rigidbody body, Vector3 relPoint, Vector3 axis)
    {
        return InvMassAtPointAxis(new ReBody(body), relPoint, axis);
    }

    // calculate effective tensor for rotation around specific axis
    public static float TensorAroundAxis(ReBody body, Vector3 axis)
    {
        //var invInertiaDiagLocal = InvInertiaTensor(body);
        var m_aJ = Quaternion.Inverse(body.rotation * body.inertiaTensorRotation) * axis;
        return Vector3.Dot(Vector3.Scale(body.inertiaTensor, m_aJ), m_aJ);
    }

    public static float TensorAroundAxis(Rigidbody body, Vector3 axis)
    {
        return TensorAroundAxis(new ReBody(body), axis);
    }

    // calculate effective for rotation around axis at particular point
    public static float TensorAtPointAxis(ReBody body, Vector3 relPoint, Vector3 axis)
    {
        //var invInertiaDiagLocal = InvInertiaTensor(body);
        //var m_aJ = Quaternion.Inverse(body.rotation * body.inertiaTensorRotation) * axis;
        //return Vector3.Dot(Vector3.Scale(body.inertiaTensor, m_aJ), m_aJ)
        return TensorAroundAxis(body,axis) + Vector3.ProjectOnPlane(relPoint, axis).sqrMagnitude * body.mass;
    }

    public static float TensorAtPointAxis(Rigidbody body, Vector3 relPoint, Vector3 axis)
    {
        return TensorAtPointAxis(new ReBody(body), relPoint, axis);
    }


    // Linear constant deceleration algorithm - returns acceleration to apply this frame.
    // Assume that the body located at "dist" from target will be braking at constant deceleration "maxA" for "n" frames startgin from the next frame.
    // 1. Calculate "n" - steps needed to stop from current "dist" using "maxA",
    // 2. Calculate "a" - acceleration to apply current frame that will make body stoppable over "dist" in next "n" frames. Includes killing all the current velocity "vel".

    public static float ConstantDeceleration1D(float dist, float vel, float maxA, float dt)
    {
        return ConstantDeceleration1D(dist, vel, maxA, dt, out _);
    }

    public static float ConstantDeceleration1D(float dist, float vel, float maxA, float dt, out float scaling)
    {
        // calculate amount of steps to brake at current distance
        var n = Mathf.Ceil((-3 + Mathf.Sqrt(1 + 8 * dist / dt / dt / maxA)) / 2);
        float a;
        if (n <= 0) //if n<0 we're at position, treat as last step
        {
            var mult1 = .5f;
            var mult2 = .75f;// 1;// .5f;

            a = -dist / dt / dt * mult1 - vel / dt * mult2;
        }
        else // calculate acceleration to apply at current step to end up in position stoppable in n steps
            a = -dist / (n + 1) / dt / dt - n / 2 * maxA - vel / dt;
        if (a == 0)
        {
            scaling = 1;
            return a;
        }
        else if (a > 0) // braking, be a bit more relaxed
        {
            var clamped = ClampExtensions.SoftClamp(a, maxA * offAxisMultiplier, softClampFalloff);
            scaling = clamped / a;
            return clamped;
        }
        else // accelerate more than allowed, clamp
        {
            var clamped = ClampExtensions.SoftClamp(a, maxA, softClampFalloff);
            scaling = clamped / a;
            return clamped;
        }

        
    }

    public static float ConstantDeceleration(float pos, float vel, float targetVel, float externalA, float maxA, float maxVel, float dt)
    {
        var axis = pos >= 0 ? 1 : -1;

        var posError = Mathf.Abs(pos);
        var velError = axis * (vel - targetVel);

        maxA += axis * externalA;

        var a = ConstantDeceleration1D(posError, velError, maxA, dt);
        if (maxVel > 0)
        {
            var desiredVel = axis * (a * dt + velError) + targetVel;
#if SOFTCLAMP
            desiredVel = ClampExtensions.SoftClamp(desiredVel, maxVel, softClampFalloff);
            var acceleration = (desiredVel - vel) / dt - externalA;


            //acceleration = acceleration.SoftClampMagnitude(maxA,.softClampFalloff);
            return acceleration;
#else
        desiredVel = Vector3.ClampMagnitude(desiredVel, maxVel);
        var acceleration = (desiredVel - vel) / dt - externalA;

        acceleration = Vector3.ClampMagnitude(acceleration, maxA);
        return acceleration;
#endif
        }
        else
            return axis*a;
    }

    // Full constant deceleration algorithm - can match target velocity
    public static Vector3 ConstantDeceleration(Vector3 pos, Vector3 vel, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel, float dt)
    {
        return ConstantDeceleration(pos, vel, targetVel, externalA, maxA, maxVel, dt, out _);
    }
    public static Vector3 ConstantDeceleration(Vector3 pos, Vector3 vel, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel, float dt, out float scaling)
    {
        var axis = (pos + 0 * dt * vel).normalized;

        var posError = pos.magnitude;
        var velError = Vector3.Dot(vel - targetVel, axis);

        maxA += Mathf.Clamp(Vector3.Dot(externalA, axis), 0, float.MaxValue);

        var a = ConstantDeceleration1D(posError, velError, maxA, dt, out var scaling1);
        var offAxisA = -(Vector3.ProjectOnPlane(vel - targetVel, axis) / dt); // acceleration to stop all off axis motion
                                                                              //        offAxisA = offAxisA.SoftClampMagnitude(maxA* offAxisMultiplier, softClampFalloff);
        offAxisA = offAxisA.Clamp(maxA * offAxisMultiplier);

        var desiredVel = vel + axis * a * dt + offAxisA * dt;
#if SOFTCLAMP
        desiredVel = desiredVel.SoftClampMagnitude(maxVel, softClampFalloff, out scaling);
        var acceleration = (desiredVel - vel) / dt - externalA;

        scaling *= scaling1;
        //acceleration = acceleration.SoftClampMagnitude(maxA,.softClampFalloff);
        return acceleration;
#else
        desiredVel = Vector3.ClampMagnitude(desiredVel, maxVel);
        var acceleration = (desiredVel - vel) / dt - externalA;

        acceleration = Vector3.ClampMagnitude(acceleration, maxA);
        return acceleration;
#endif
    }
    // Controller aligning "anchor" located on body "body" with world "targetPos" and "targetVel". Limits are respected:
    // * maxForce - maximum force applied at anchor point
    // * maxVel - maximum velocity of the anchor point
    // * maxA - maximum deceleration when body should be braking to arrive at targetPos
    public static void ApplyConstantDecelerationLinear(ReBody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel)
    {

        var pos = worldAnchor - targetPos;
        var acceleration = ConstantDeceleration(pos, body.GetPointVelocity(worldAnchor), targetVel, externalA, maxA, maxVel, Time.fixedDeltaTime);

        body.AddLinearAccelerationAtPosition(acceleration, worldAnchor);
    }

    public static void ApplyConstantDecelerationLinear(Rigidbody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel)
    {
        ApplyConstantDecelerationLinear(new ReBody(body), worldAnchor, targetPos, targetVel, externalA, maxA, maxVel);
    }

    public static void ApplyConstantDecelerationLinearProjected(Vector3 normal, ReBody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel)
    {

        var pos = worldAnchor - targetPos;
        var acceleration = ConstantDeceleration(Vector3.Project(pos,normal), Vector3.Project(body.GetPointVelocity(worldAnchor),normal), Vector3.Project(targetVel,normal), Vector3.Project(externalA,normal), maxA, maxVel, Time.fixedDeltaTime);

        body.AddLinearAccelerationAtPosition(acceleration, worldAnchor);
    }

    public static void ApplyConstantDecelerationLinearProjected(Vector3 normal, Rigidbody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel)
    {
        ApplyConstantDecelerationLinearProjected(normal, new ReBody(body), worldAnchor, targetPos, targetVel, externalA, maxA, maxVel);
    }

    public static void ApplyConstantDecelerationLinearProjectedOnPlane(Vector3 normal, ReBody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel, float multiplier = 1f)
    {
        var pos = worldAnchor - targetPos;
        var acceleration = ConstantDeceleration(Vector3.ProjectOnPlane(pos, normal), Vector3.ProjectOnPlane(body.GetPointVelocity(worldAnchor), normal), Vector3.ProjectOnPlane(targetVel, normal), Vector3.ProjectOnPlane(externalA, normal), maxA, maxVel, Time.fixedDeltaTime);

        body.AddLinearAccelerationAtPosition(acceleration * multiplier, worldAnchor);
    }

    public static void ApplyConstantDecelerationLinearProjectedOnPlane(Vector3 normal, Rigidbody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, Vector3 externalA, float maxA, float maxVel, float multiplier = 1f)
    {
        ApplyConstantDecelerationLinearProjectedOnPlane(normal, new ReBody(body), worldAnchor, targetPos, targetVel, externalA, maxA, maxVel, multiplier);
    }

    // Constant angular decceleration around anchor
    public static void ApplyConstantDecelerationAngular(ReBody body, Vector3 worldAnchor, Quaternion targetRot, Vector3 targetAngularVel, float maxAngularA, float maxAngularVel)
    {

        // calculate desired angular acceleration
        var angPos = (body.rotation * Quaternion.Inverse(targetRot)).QToAngleAxis();
        var angAcceleration = ConstantDeceleration(angPos, body.angularVelocity, targetAngularVel, Vector3.zero, maxAngularA, maxAngularVel, Time.fixedDeltaTime);

        body.AddAngularAccelerationAtPosition(angAcceleration, worldAnchor);
    }

    public static void ApplyConstantDecelerationAngular(Rigidbody body, Vector3 worldAnchor, Quaternion targetRot, Vector3 targetAngularVel, float maxAngularA, float maxAngularVel)
    {
        ApplyConstantDecelerationAngular(new ReBody(body), worldAnchor, targetRot, targetAngularVel, maxAngularA, maxAngularVel);
    }

    public static void ApplyConstantDecelerationAngularProjected(Vector3 normal, ReBody body, Vector3 worldAnchor, Quaternion targetRot, Vector3 targetAngularVel, float maxAngularA, float maxAngularVel)
    {

        // calculate desired angular acceleration
        var angPos = (body.rotation * Quaternion.Inverse(targetRot)).QToAngleAxis();
        var angAcceleration = ConstantDeceleration(Vector3.Project(angPos,normal), Vector3.Project(body.angularVelocity,normal), Vector3.Project(targetAngularVel,normal), Vector3.zero, maxAngularA, maxAngularVel, Time.fixedDeltaTime);

        body.AddAngularAccelerationAtPosition(angAcceleration, worldAnchor);
    }

    public static void ApplyConstantDecelerationAngularProjected(Vector3 normal, Rigidbody body, Vector3 worldAnchor, Quaternion targetRot, Vector3 targetAngularVel, float maxAngularA, float maxAngularVel)
    {
        ApplyConstantDecelerationAngularProjected(normal, new ReBody(body), worldAnchor, targetRot, targetAngularVel, maxAngularA, maxAngularVel);
    }

    // Full constant deceleration controller for a joint having 6 degrees of control

    public static void ApplyConstantDeceleration(ReBody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, float maxA, float maxVel, Quaternion targetRot, Vector3 targetAngularVel, float maxAngularA, float maxAngularVel)
    {
        var dt = Time.fixedDeltaTime;

        var vel = body.GetPointVelocity(worldAnchor);
        var pos = worldAnchor - targetPos;
        var externalAcceleration = body.useGravity ? Physics.gravity : Vector3.zero;
        var acceleration = ConstantDeceleration(pos, vel, targetVel, externalAcceleration, maxA, maxVel, Time.fixedDeltaTime, out var scaling);

        //var linearTension =  Mathf.InverseLerp(.1f, .5f, pos.magnitude);// how much pull is on linear
        // calculate angular velocity resulting from linear motion
        var tensor = FixTensor(body.mass, body.inertiaTensor);

        var I = Inertia.Assemble(body.mass, tensor, body.rotation * body.inertiaTensorRotation).Translate(worldAnchor - body.worldCenterOfMass);
        var goodI = Inertia.FromRigidAtPoint(body, worldAnchor);
        var angularDueLinear = I.LinearAccelerationToAngular(acceleration); // 

        var angVel = body.angularVelocity + angularDueLinear * dt; // counteract angular response of linear motion
        var angPos = (body.rotation * Quaternion.Inverse(targetRot)).QToAngleAxis();
        var angAcceleration = ConstantDeceleration(angPos, angVel, targetAngularVel, Vector3.zero, maxAngularA, maxAngularVel, Time.fixedDeltaTime);

        var totalA = new Vector6((angAcceleration+angularDueLinear) * scaling, acceleration);
        var totalF = goodI * totalA;
        var fCG = new PluckerTranslate(body.worldCenterOfMass - worldAnchor).TransformForce(totalF);

        body.AddForce(fCG.linear, ForceMode.Force);
        body.AddTorque(fCG.angular, ForceMode.Force);
    }

    public static void ApplyConstantDeceleration(Rigidbody body, Vector3 worldAnchor, Vector3 targetPos, Vector3 targetVel, float maxA, float maxVel, Quaternion targetRot, Vector3 targetAngularVel, float maxAngularA, float maxAngularVel)
    {
        ApplyConstantDeceleration(new ReBody(body), worldAnchor, targetPos, targetVel, maxA, maxVel, targetRot, targetAngularVel, maxAngularA, maxAngularVel);
    }

    static Vector3 FixTensor(float mass, Vector3 tensor)
    {
        // ALT1: just prevent skew (not enough)
        //var max = Mathf.Max(Mathf.Max(tensor.x, tensor.y), tensor.z) / 10; // unskew
        //tensor = new Vector3(Mathf.Max(tensor.x, max), Mathf.Max(tensor.y, max), Mathf.Max(tensor.z, max));

        //tensor of 1m cube
        var side = 1f;
        var minimumAllowed = mass * side * side / 6;

        // ALT2: scale tensor to ensure minimum
        var minimumActual = Mathf.Min(Mathf.Min(tensor.x, tensor.y), tensor.z);
        if (minimumActual < minimumAllowed)
            return tensor * minimumAllowed / minimumActual;
        else
            return tensor;

        // ALT3: clamp each side to ensure minimum
        //tensor = new Vector3(Mathf.Max(tensor.x, minimumAllowed), Mathf.Max(tensor.y, minimumAllowed), Mathf.Max(tensor.z, minimumAllowed));
    }




    public static Vector6 CalculateConstantDecceleration(Vector3 pos, Vector3 target, Quaternion rot, Quaternion targetRot, Vector6 velocity, Vector6 targetVel, float maxA, float maxVel,  float maxAngularA, float maxAngularVel)
    {
        var dt = Time.fixedDeltaTime;
        var errorPos = pos - target;
        // calculate desired linear acceleration
        var acceleration = ConstantDeceleration(pos-target, velocity.linear, targetVel.linear, Physics.gravity, maxA, maxVel, dt);
        var angAcceleration = ConstantDeceleration((rot * Quaternion.Inverse(targetRot)).QToAngleAxis(), velocity.angular, targetVel.angular, Vector3.zero, maxAngularA, maxAngularVel, dt);

#if SOFTCLAMP
        //if (acceleration.magnitude > maxA)
        //{
        //    var mult = Mathf.Pow(acceleration.magnitude / maxA, 0.1f) * maxA / acceleration.magnitude;
        //    acceleration *= mult;
        //    angAcceleration *= mult;
        //    angAcceleration = angAcceleration.SoftClampMagnitude(maxAngularA, softClampFalloff);
        //}
#endif
        return new Vector6(angAcceleration, acceleration);
    }

    public static void AddAccelerationAtPosition(this ReBody body, Vector6 acc, Vector3 worldAnchor)
    {
        // apply linear acceleration
        //body.AddForceAtPosition(acc.linear / InvMassAtPointAxis(body, worldAnchor - body.worldCenterOfMass, acc.linear.normalized), worldAnchor);

        AddLinearAccelerationAtPosition(body, acc.linear, worldAnchor);
        AddAngularAccelerationAtPosition(body, acc.angular, worldAnchor);
    }

    public static void AddAccelerationAtPosition(this Rigidbody body, Vector6 acc, Vector3 worldAnchor)
    {
        var reBody = new ReBody(body);
        reBody.AddAccelerationAtPosition(acc, worldAnchor);
    }

    public static void AddTorqueAtPosition(this ReBody body, Vector3 torque, Vector3 worldAnchor)
    {
        var f = new Vector6(torque, Vector3.zero);
        var fCG = new PluckerTranslate(body.worldCenterOfMass - worldAnchor).TransformForce(f);
        body.AddTorque(fCG.angular);
        body.AddForce(fCG.linear);
    }

    public static void AddTorqueAtPosition(this Rigidbody body, Vector3 torque, Vector3 worldAnchor)
    {
        var reBody = new ReBody(body);
        reBody.AddTorqueAtPosition(torque, worldAnchor);
    }

    public static void AddAngularAccelerationAtPosition(this ReBody body, Vector3 acc, Vector3 worldAnchor)
    {
        if (acc.magnitude < 0.001f) return;

        //// calculate force at anchor to give that acceleration, just use angular as linear is handled separately
        var a = new Vector6(acc, Vector3.zero);//, acceleration); 
        //var I = Inertia.FromRigidAtPoint(body, worldAnchor);
        //var f = I * a;
        //// transform to CG and apply
        //var fCG = new PluckerTranslate(body.worldCenterOfMass - worldAnchor).TransformForce(f);
        //body.AddTorque(fCG.FirstVector3());
        //body.AddForce(fCG.SecondVector3());


        // skip building full inertia
        var aCG = new PluckerTranslate(body.worldCenterOfMass - worldAnchor).TransformVelocity(a);
        var tensorToWorld = body.rotation * body.inertiaTensorRotation;
        var aTensor = Quaternion.Inverse(tensorToWorld) * aCG.angular;
        var fTensor = Vector3.Scale(aTensor, body.inertiaTensor);
        var torque = tensorToWorld * fTensor;
        body.AddTorque(torque);
        body.AddForce(aCG.linear * body.mass);
    }


    public static void AddAngularAccelerationAtPosition(this Rigidbody body, Vector3 acc, Vector3 worldAnchor)
    {
        var reBody = new ReBody(body);
        reBody.AddAngularAccelerationAtPosition(acc, worldAnchor);
    }

    public static void AddLinearAccelerationAtPosition(this ReBody body, Vector3 acc, Vector3 worldAnchor)
    {
        if (acc.magnitude < 0.001f) return;
    
        body.AddForceAtPosition(acc / InvMassAtPointAxis(body, worldAnchor - body.worldCenterOfMass, acc.normalized), worldAnchor);

        //var a = new Vector6( Vector3.zero,acc);//, acceleration); 
        //var I = Inertia.FromRigidAtPoint(body, worldAnchor);
        //var f = I * a;
        //var fCG = new PluckerTranslate(body.worldCenterOfMass - worldAnchor).TransformForce(f);
        //body.AddTorque(fCG.FirstVector3());
        //body.AddForce(fCG.SecondVector3());
    }

    public static void AddLinearAccelerationAtPosition(this Rigidbody body, Vector3 acc, Vector3 worldAnchor)
    {
        var reBody = new ReBody(body);
        reBody.AddLinearAccelerationAtPosition(acc, worldAnchor);
    }



    public static Vector6 GetPointVelocity(ReBody body, Vector3 worldPos)
    {
        return new Vector6(body.angularVelocity, body.GetPointVelocity(worldPos));
    }

}
