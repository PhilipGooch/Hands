using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Recoil
{
    public struct ForceVector
    {

        public float3 angular;
        public float3 linear;
        public ForceVector justAngular => new ForceVector(angular, float3.zero);
        public ForceVector justLinear => new ForceVector(float3.zero, linear);
        public static readonly ForceVector zero = new ForceVector(new float3(0), new float3(0));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ForceVector(float3 angular, float3 linear)
        {
            this.angular = angular;
            this.linear = linear;
        }

        public static ForceVector Linear(float3 l) => new ForceVector(float3.zero, l);
        public static ForceVector Linear(float x, float y, float z) => new ForceVector(float3.zero, new float3(x, y, z));
        public static ForceVector Angular(float3 a) => new ForceVector(a, float3.zero);
        public static ForceVector Angular(float x, float y, float z) => new ForceVector(new float3(x, y, z), float3.zero);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForceVector operator *(ForceVector lhs, float rhs)
        {
            return new ForceVector(lhs.angular * rhs, lhs.linear * rhs);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForceVector operator *(float rhs, ForceVector lhs)
        {
            return new ForceVector(lhs.angular * rhs, lhs.linear * rhs);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForceVector operator +(ForceVector lhs, ForceVector rhs)
        {
            return new ForceVector(lhs.angular + rhs.angular, lhs.linear + rhs.linear);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForceVector operator -(ForceVector lhs, ForceVector rhs)
        {
            return new ForceVector(lhs.angular - rhs.angular, lhs.linear - rhs.linear);
        }

        public static ForceVector operator -(ForceVector rhs)
        {
            return new ForceVector(-rhs.angular, -rhs.linear);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(ForceVector lhs, ForceVector rhs)
        {
            return math.dot(lhs.angular, rhs.angular) + math.dot(lhs.linear, rhs.linear);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForceVector operator *(RigidTransform x, ForceVector f)
        {
            return new ForceVector(math.rotate(x.rot, f.angular - math.cross(x.pos, f.linear)),
                math.rotate(x.rot, f.linear));

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ForceVector TranslateBy(float3 x)
        {
            return new ForceVector(angular - math.cross(x, linear), linear);
        }
        public ForceVector rotate(quaternion q)
        {
            return new ForceVector(math.rotate(q,angular),math.rotate(q,linear));
        }
        public float magnitude => math.sqrt(math.lengthsq(angular) + math.lengthsq(linear));

        public float sqrMagnitude => math.lengthsq(angular) + math.lengthsq(linear);

        public override string ToString()
        {
            return $"(({angular.x:F5}, {angular.y:F5}, {angular.z:F5}), ({linear.x:F5}, {linear.y:F5}, {linear.z:F5}))";
        }
    }
}