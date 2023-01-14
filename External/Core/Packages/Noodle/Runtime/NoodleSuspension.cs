using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public struct NoodleSuspensionData
    {
        // spring config
        public int ballLink;
        public int link;
        public float3 anchor;

        public float3 axis;
        public float tonus;
        //public float maxLen;

        // target
        public float targetPosition;

        // state
        public float invM;
        public float ballInvM;

        public bool isFree;
        public float bias;
        public float gamma;
        public float impulse;

        // limit state
        public float biasLimit;
        public float limitImpulse;
    }


    // Spring has two constraints:
    // - hard limit at targetPosition preventing overextenstion
    // - soft spring targeting position beyond targetPosition, distance is calculated to support weight of the character and more
    public static class NoodleSuspension
    {
        public static unsafe void CalculateJacobian(ref NoodleSuspensionData suspension, in Articulation articulation, in NoodleDimensions dim, bool grounded)
        {
            float designGravity = 9.81f;
            var bodies = articulation.GetBodies();
            //var ragdollMass = bodies.CalculateMass(1, 12);
            var h = World.main.dt;
            var kp = dim.suspendedMass * designGravity / NoodleConstants.SUSPENSION_TENSION; // support ragdollmass at tension
            var kd = 2 * math.sqrt(kp * dim.suspendedMass); // critically damped
            var targetTension = NoodleConstants.SUSPENSION_TENSION + dim.suspendedMass * designGravity / kp;

            var spring = new Spring(kp, kd)*suspension.tonus;
            suspension.isFree = spring.isFree;
            
            ref var ballBody = ref bodies.GetBody(suspension.ballLink);
            ref var body = ref bodies.GetBody(suspension.link);

            // error
            var xBall = ballBody.x.pos;
            var x = body.TransformPoint(suspension.anchor);
            //data.axis = math.normalize(x - xBall);
            var offset = math.dot(x - xBall, suspension.axis);

            // jacobian
            suspension.ballInvM = ballBody.invM;
            var response = Articulation.ComputeImpulseResponseFast(articulation, suspension.link, suspension.anchor, suspension.axis, default);
            suspension.invM = math.dot(response, suspension.axis);

            // softspring
            var C = offset - (suspension.targetPosition + targetTension);// *suspension.tonus;
            
            spring.CalculateSpring(C, 0, h, out suspension.gamma, out suspension.bias);

            // hard limit
            var Cmax = offset - suspension.targetPosition;
            suspension.biasLimit = Cmax / h;

            suspension.impulse = 0f;
            suspension.limitImpulse = 0f;
        }

        public static unsafe void VelocityIteration(ref NoodleSuspensionData suspension, in Articulation articulation, bool grounded, float3 groundVelocity)
        {
            var world = World.main;
            var bodies = articulation.GetBodies();

            var v = math.dot(bodies.GetLocalPointVelocity(suspension.link, suspension.anchor).linear, suspension.axis);
            var vBall =  math.dot(bodies.GetLocalPointVelocity(suspension.ballLink, float3.zero).linear, suspension.axis);

            v += math.dot(world.gravity, suspension.axis) * world.dt; // bias
            vBall += math.dot(world.gravity, suspension.axis) * world.dt; // bias

            //STEP1. Solve everything as ragdoll is supported only by ball inertia (in air)
            var jMult = suspension.isFree ? 0 : 1;

            // soft constraint velocity iteration
            var Jv = v - vBall;
            var softMass = 1 / (suspension.invM + suspension.ballInvM + suspension.gamma);
            var impulseDelta = -softMass * (Jv * jMult + suspension.bias + suspension.gamma * suspension.impulse);

            // limit velocity iteration
            var mass = 1 / (suspension.invM + suspension.ballInvM);
            var JvLimit = (Jv + impulseDelta / mass);

            var limitDelta = -mass * (JvLimit + suspension.biasLimit); // impulse to stop at limit
            var limitImpulse = math.min(0.0f, suspension.limitImpulse + limitDelta); // total limiting impulse cant be positive
            limitDelta = limitImpulse - suspension.limitImpulse;

            // STEP2. if predicted velocity of ball is down and it was grounded - it will still be grounded
            // so we could assume the spring is supported by ground itself - and redo all calculations for infinite ball mass
            if (grounded && vBall - suspension.ballInvM * (impulseDelta + limitDelta) < groundVelocity.y) 
            {
                // calculate soft spring as moving just the ragdoll mass against moving ground
                Jv = v - groundVelocity.y;
                softMass = 1 / (suspension.invM + suspension.gamma);
                impulseDelta = -softMass * (Jv * jMult + suspension.bias + suspension.gamma * suspension.impulse);

                // dont push agains the limit
                mass = 1 / suspension.invM;
                JvLimit = Jv + impulseDelta / mass;

                limitDelta = -mass * (JvLimit + suspension.biasLimit);
                limitImpulse = math.min(0.0f, suspension.limitImpulse + limitDelta); // total limiting impulse cant be positive
                limitDelta = limitImpulse - suspension.limitImpulse;
            }

            // apply impulse
            bodies.AddLinearVelocity(suspension.ballLink, -suspension.axis * suspension.ballInvM * (impulseDelta + limitDelta));
            var _v = articulation.ExtractWorldVelocityCopy();
            using (var context = articulation.GetContext(_v))
                articulation.ApplyImpulse(context, suspension.link, suspension.anchor, suspension.axis * (impulseDelta + limitDelta), true);
            articulation.WriteAndDisposeVelocityCopy(_v);

            // store for next iteration
            suspension.impulse += impulseDelta;
            suspension.limitImpulse += limitDelta;

        }
    }
}