using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Recoil
{
    public struct MotionVector
    {

        public float3 angular;
        public float3 linear;
        public MotionVector justAngular => new MotionVector(angular, float3.zero);
        public MotionVector justLinear => new MotionVector(float3.zero, linear);

        public static readonly MotionVector zero = new MotionVector(new float3(0), new float3(0));

        public MotionVector(float3 angular, float3 linear)
        {
            this.angular = angular;
            this.linear = linear;
        }

        
        public static MotionVector Linear(float3 l) => new MotionVector(float3.zero, l);
        public static MotionVector Linear(float x, float y, float z) => new MotionVector(float3.zero, new float3(x,y,z));
        public static MotionVector Angular(float3 a) => new MotionVector(a, float3.zero);
        public static MotionVector Angular(float x, float y, float z) => new MotionVector(new float3(x, y, z), float3.zero);


        public static MotionVector operator *(MotionVector lhs, float rhs)
        {
            return new MotionVector(lhs.angular * rhs, lhs.linear * rhs);

        }
        public static MotionVector operator /(MotionVector lhs, float rhs)
        {
            return new MotionVector(lhs.angular / rhs, lhs.linear / rhs);

        }
        public static MotionVector operator *(float rhs, MotionVector lhs)
        {
            return new MotionVector(lhs.angular * rhs, lhs.linear * rhs);

        }

        public static MotionVector operator +(MotionVector lhs, MotionVector rhs)
        {
            return new MotionVector(lhs.angular + rhs.angular, lhs.linear + rhs.linear);
        }

        public static MotionVector operator -(MotionVector lhs, MotionVector rhs)
        {
            return new MotionVector(lhs.angular - rhs.angular, lhs.linear - rhs.linear);
        }

        public static MotionVector operator -(MotionVector rhs)
        {
            return new MotionVector(-rhs.angular, -rhs.linear);
        }

        public static float Dot(MotionVector lhs, MotionVector rhs)
        {
            return math.dot(lhs.angular, rhs.angular) + math.dot(lhs.linear, rhs.linear);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MotionVector operator *(RigidTransform x, MotionVector v)
        {
            return new MotionVector(math.rotate(x.rot, v.angular),
                math.rotate(x.rot, v.linear - math.cross(x.pos, v.angular)));

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MotionVector TranslateBy(float3 x)
        {
            return new MotionVector(angular, linear - math.cross(x, angular));
        }

        public float magnitude => math.sqrt(math.lengthsq(angular) + math.lengthsq(linear));

        public float sqrMagnitude => math.lengthsq(angular) + math.lengthsq(linear);

        public override string ToString()
        {
            return $"(({angular.x:F5}, {angular.y:F5}, {angular.z:F5}), ({linear.x:F5}, {linear.y:F5}, {linear.z:F5}))";
        }
        public MotionVector rotate(quaternion q)
        {
            return new MotionVector(math.rotate(q,angular),math.rotate(q,linear));
        }
        public MotionVector crossAngular(float3 a)
        {

            return new MotionVector(math.cross(angular, a), math.cross(linear, a));
        }

        public MotionVector cross(MotionVector v2)
        {

            return new MotionVector(math.cross(angular, v2.angular),
                math.cross(angular, v2.linear) + math.cross(linear, v2.angular));
        }

        public ForceVector cross(ForceVector v2)
        {

            return new ForceVector(math.cross(angular, v2.angular) + math.cross(linear, v2.linear),
                math.cross(angular, v2.linear));
        }


    }
}