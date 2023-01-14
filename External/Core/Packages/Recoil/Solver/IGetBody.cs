using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public interface IGetBody
    {
        int count { get; }
        ref Body GetBody(int link);
        // gets reference to velocity for a body in specific context: world stores it directly in body, but articulation and block solvers operate on copy of velocity
        ref Velocity4 GetVelocity4(int link); 
    }
    public static class BodyExtensions
    {
        public static float3 TransformPoint(this Body body, float3 anchor)
            => math.transform(body.x, anchor);
        public static float3 TransformDirection(this Body body, float3 dir)
            => math.rotate(body.x.rot, dir);
        public static float3 InverseTransformPoint(this Body body, float3 worldPos)
            => math.transform(math.inverse(body.x), worldPos);
        public static float3 InverseTransformDirection(this Body body, float3 worldDir)
            => math.rotate(math.inverse(body.x.rot), worldDir);
    }


    public static class IGetBodyExtensions
    {
        // transforms
        public static float3 TransformPoint<T>(this T bodies, int link, float3 anchor) where T : IGetBody
            => link >= 0 ? bodies.GetBody(link).TransformPoint(anchor) : anchor;
        public static float3 TransformDirection<T>(this T bodies, int link, float3 dir) where T : IGetBody
            => link >= 0 ? bodies.GetBody(link).TransformDirection(dir) : dir;
        public static float3 InverseTransformPoint<T>(this T bodies, int link, float3 worldPos) where T : IGetBody
            => link >= 0 ? bodies.GetBody(link).InverseTransformPoint(worldPos) : worldPos;

        public static float3 InverseTransformDirection<T>(this T bodies, int link, float3 worldDir) where T : IGetBody
            => link >= 0 ? bodies.GetBody(link).InverseTransformDirection(worldDir) : worldDir;

        // Velocity functions

        public static MotionVector GetVelocity<T>(this T context, int link) where T : IGetBody
            => link >= 0 ? (MotionVector)context.GetVelocity4(link) : MotionVector.zero;
        public static MotionVector GetRelativePointVelocity<T>(this T context, int link, float3 r) where T : IGetBody
            => link >= 0 ? context.GetVelocity(link).TranslateBy(r) : MotionVector.zero;
        public static MotionVector GetLocalPointVelocity<T>(this T context, int link, float3 anchor) where T : IGetBody
            => link >= 0 ? context.GetRelativePointVelocity(link, math.rotate(context.GetBody(link).x.rot, anchor)) : MotionVector.zero;
        public static MotionVector GetWorldPointVelocity<T>(this T context, int link, float3 anchor) where T : IGetBody
            => link >= 0 ? context.GetRelativePointVelocity(link, anchor- context.GetBody(link).x.pos) : MotionVector.zero;

        public static void SetVelocity<T>(this T context, int link, MotionVector vel) where T : IGetBody
        {
            SetVelocity4(context, link, (Velocity4)vel);
        }
        public static void SetVelocity4<T>(this T context, int link, Velocity4 vel) where T : IGetBody
        {
            if (!math.isfinite(math.csum(vel.angular3)) || !math.isfinite(math.csum(vel.linear)))
                LogInfiniteVelocity();
            else
                context.GetVelocity4(link) = vel;
        }
        [BurstDiscard]
        static void LogInfiniteVelocity()
        {
            Debug.LogError("Infinite velocity");
        }

     

        // Acceleration
        public static void AddVelocity<T>(this T context, int bodyId, Velocity4 delta) where T : IGetBody
        {
            context.SetVelocity4(bodyId, context.GetVelocity4(bodyId) + delta);
        }
        public static void AddVelocity<T>(this T context, int bodyId, MotionVector delta) where T : IGetBody
        {
            context.AddVelocity(bodyId, (Velocity4)delta);
        }
        public static void AddAngularVelocity<T>(this T context, int bodyId, float3 delta) where T : IGetBody
        {
            context.AddVelocity(bodyId, MotionVector.Angular(delta));

        }
        public static void AddLinearVelocity<T>(this T context, int bodyId, float3 delta) where T : IGetBody
        {
            context.AddVelocity(bodyId, MotionVector.Linear(delta));
        }
        // Impulse functions
        public static void ApplyImpulse<T>(this T context, int bodyId, ForceVector impulse) where T : IGetBody
        {
            ref var body = ref context.GetBody(bodyId);
            context.AddVelocity(bodyId, new MotionVector(re.mul(body.invI, impulse.angular), body.invM * impulse.linear));
        }
        public static void ApplyImpulse<T>(this T context, int bodyId, float3 linear) where T : IGetBody
        {
            ref var body = ref context.GetBody(bodyId);
            context.AddLinearVelocity(bodyId, body.invM * linear);
        }
        public static void ApplyImpulseAtWorldPos<T>(this T context, int bodyId, float3 impulse, float3 worldPos) where T : IGetBody
        {
            ref var body = ref context.GetBody(bodyId);
            var r = worldPos - body.x.pos;
            context.ApplyImpulseAtRelativePoint(bodyId, impulse, r);
        }
        public static void ApplyImpulseAtRelativePoint<T>(this T context, int bodyId, float3 impulse, float3 r) where T : IGetBody
        {
            ref var body = ref context.GetBody(bodyId);
            context.AddVelocity(bodyId, new MotionVector(re.mul(body.invI, math.cross(r, impulse)), body.invM * impulse));
        }
        public static void ApplyImpulseAtLocalPoint<T>(this T context, int bodyId, float3 impulse, float3 anchor) where T : IGetBody
        {
            ref var body = ref context.GetBody(bodyId);
            var r = math.rotate(body.x.rot, anchor);
            context.ApplyImpulseAtRelativePoint(bodyId, impulse, r);
        }

        // Force functions, convert to impulse and apply

        public static void ApplyForce<T>(this T context, int bodyId, ForceVector impulse) where T : IGetBody =>
            ApplyImpulse(context, bodyId, impulse * World.main.dt);
        public static void ApplyForce<T>(this T context, int bodyId, float3 linear) where T : IGetBody =>
            ApplyImpulse(context, bodyId, linear * World.main.dt);
        public static void ApplyForceAtWorldPos<T>(this T context, int bodyId, float3 impulse, float3 worldPos) where T : IGetBody =>
            ApplyImpulseAtWorldPos(context, bodyId, impulse * World.main.dt, worldPos);

        public static void ApplyForceAtRelativePoint<T>(this T context, int bodyId, float3 impulse, float3 r) where T : IGetBody =>
            ApplyImpulseAtRelativePoint(context, bodyId, impulse * World.main.dt, r);

        public static void ApplyForceAtLocalPoint<T>(this T context, int bodyId, float3 impulse, float3 anchor) where T : IGetBody =>
            ApplyImpulseAtLocalPoint(context, bodyId, impulse * World.main.dt, anchor);

        // Inertia functions
        public static void GetInverseRigidInertia<T>(this T context, int link, out float invM, out lt3x3 invI) where T : IGetBody
        {
            ref var body = ref context.GetBody(link);
            invM = body.invM;
            invI = body.invI;
        }

        public static float CalculateMass<T>(this T bodies, int start, int count) where T : IGetBody
        {
            var totalMass = 0f;
            for (var b = start; b < start + count; b++)
            {
                ref var body = ref bodies.GetBody(b);
                totalMass += body.m;

            }
            return totalMass;
        }
        // calculates center of mass for a range of bodies
        public static float3 CalculateCenterOfMass<T>(this T bodies, int start, int count) where T : IGetBody
        {
            var total = float3.zero;
            var totalMass = 0f;
            for (var b = start; b < start + count; b++)
            {
                ref var body = ref bodies.GetBody(b);
                totalMass += body.m;
                total += body.m * body.x.pos;

            }
            return total / totalMass;
        }

        // calculates velocity for a range of bodies by dividing total impulse by mass
        public static float3 CalculateVelocity<T>(this T bodies, int start, int count) where T : IGetBody
        {
            var total = float3.zero;
            var totalMass = 0f;
            for (var link = start; link < start + count; link++)
            {
                //var body = solver.links[link];
                var m = bodies.GetBody(link).m;
                total += m * bodies.GetVelocity(link).linear;
                totalMass += m;
            }
            return total / totalMass;
        }

    }
}
