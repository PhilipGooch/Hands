using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Recoil
{
    public struct RigidBodyInertia
    {
        public float m;
        public float3 h;
        public lt3x3 I;

        public RigidBodyInertia(
            float m, float3 h, lt3x3 I)
        {
            this.m = m;
            this.h = h;
            this.I = I;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static lt3x3 CalculatIFromTensor(quaternion inertiaOrientation, float3 inertiaTensor)
        {
            var tensor = float3x3.Scale(inertiaTensor);
            var rot = math.float3x3(inertiaOrientation);
            var rotInv = math.transpose(rot);
            return new lt3x3(math.mul(math.mul(rot, tensor), rotInv));
        }


        public static RigidBodyInertia Assemble(quaternion inertiaOrientation, float3 inertiaTensor, float mass,
            float3 r)
        {
            var I = CalculatIFromTensor(inertiaOrientation, inertiaTensor);
            var minusmr = -mass * r;
            return new RigidBodyInertia(mass, minusmr, I + re.ltcrosscross(minusmr, r));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RigidBodyInertia operator +(RigidBodyInertia a, RigidBodyInertia b)
        {
            return new RigidBodyInertia(a.m + b.m, a.h + b.h, a.I + b.I);
        }
        //2.74
        public RigidBodyInertia inverse => throw
            //WRONG!!!! if needed use  formula (2.74)
            //return new RigidBodyInertia(1 / m, -h / m / m, I.inverse);
            new System.InvalidOperationException();


      
        public RigidBodyInertia TranslateBy(float3 r)
        {
            var HminusrxM = h - r * m;
            return new RigidBodyInertia(m, HminusrxM, I + re.ltcrosscross(r,h) + re.ltcrosscross(HminusrxM,r));

        }
    }
}