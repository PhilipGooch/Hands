using Unity.Mathematics;
using UnityEngine;
using Recoil;
using System;

namespace Recoil
{
    public static class PhysX
    {
        public static Vector3 TransformPoint(this Rigidbody body, Vector3 pos)
        {
            return body.position + body.rotation * pos;
        }
        public static Vector3 TransformDirection(this Rigidbody body, Vector3 dir)
        {
            return body.rotation * dir;
        }

        public static Vector3 InverseTransformPoint(this Rigidbody body, Vector3 worldPos)
        {
            return Quaternion.Inverse(body.rotation) * (worldPos - body.position);
        }
        public static Vector3 InverseTransformDirection(this Rigidbody body, Vector3 worldDir)
        {
            return Quaternion.Inverse(body.rotation) * worldDir;
        }
        public static void ReadBodyAtAnchor(Rigidbody body, float3 worldAnchor, out RigidBodyInertia rbi, out RigidTransform x, out MotionVector mv, out ForceVector biasForce)
        {
            var inertiaOrientation = body.rotation * body.inertiaTensorRotation;
            x = new RigidTransform(body.rotation, worldAnchor);
            var vCG = new MotionVector(body.angularVelocity, body.velocity);
            var r = worldAnchor - (float3)body.worldCenterOfMass;
            mv = vCG.TranslateBy(r);
            rbi = RigidBodyInertia.Assemble(inertiaOrientation, body.inertiaTensor, body.mass, r);


            var aCG = vCG.justLinear.cross(vCG);
            var aFixBias = aCG.TranslateBy(-r);
            biasForce = re.mul(rbi, aFixBias);
        }

        public static void ReadBody(Rigidbody body, out RigidTransform x, out MotionVector vCG, out RigidTransform xTensor, out float3 tensor, out float mass)
        {
            x = new RigidTransform(body.rotation, body.position);
            xTensor = new RigidTransform(body.inertiaTensorRotation, body.centerOfMass);
            tensor = body.inertiaTensor;
            mass = body.mass;
            vCG = new MotionVector(body.angularVelocity, body.velocity);
        }

        public static void ApplyForceAtRelativePosition(Rigidbody body, float3 relPos, ForceVector fv)
        {
            var fCG = fv.TranslateBy(-relPos);
            body.AddForce(fCG.linear);
            body.AddTorque(fCG.angular);
        }

        public static void ApplyForce(Rigidbody body, float3 worldAnchor, ForceVector fv)
        {
            var fCG = fv.TranslateBy((float3)body.worldCenterOfMass - worldAnchor);
            body.AddForce(fCG.linear);
            body.AddTorque(fCG.angular);
            //Debug.DrawRay(worldAnchor, fv.angular / 100, Color.red);
            //Debug.DrawRay(worldAnchor, fv.linear / 100, Color.green);
        }

        public static void ApplyAcceleration(Rigidbody body, float3 worldAnchor, MotionVector mv)
        {
            mv = mv.TranslateBy((float3)body.worldCenterOfMass - worldAnchor);
            body.AddTorque(mv.angular, ForceMode.Acceleration);
            body.AddForce(mv.linear, ForceMode.Acceleration);
            //Debug.DrawRay(worldAnchor, mv.angular / 10, Color.yellow);
            //Debug.DrawRay(worldAnchor, mv.linear / 10, Color.cyan);
        }

        public static void ApplyVelocity(Rigidbody body, MotionVector mv)
        {

            body.velocity = mv.linear;
            body.angularVelocity = mv.angular;
        }

        public static RigidTransform GetRigidTransform(this Rigidbody body)
        {
            return new RigidTransform(body.rotation, body.position);
        }
        public static RigidTransform GetRigidTransformAtCenterOfMass(this Rigidbody body)
        {
            return new RigidTransform(body.rotation, body.worldCenterOfMass);
        }
        public static RigidTransform GetRigidLocalTransform(this Transform transform)
        {
            //if (transform.localScale != new Vector3(1, 1, 1)) Debug.LogError("Non Unit transform", transform);
            return new RigidTransform(transform.localRotation, transform.localPosition);
        }
        public static RigidTransform GetRigidWorldTransform(this Transform transform)
        {
            //if (transform.localScale != new Vector3(1, 1, 1)) Debug.LogError("Non Unit transform", transform);
            return new RigidTransform(transform.rotation, transform.position);
        }
    }
}