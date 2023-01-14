using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace NBG.XPBDRope
{
    [System.Serializable]
    public struct RopeProfileData
    {
        [SerializeField]
        [Tooltip("Rope radius. How thick the rope is. Directly controls the rope capsule collider radius.")]
        float radius;
        [SerializeField]
        [Tooltip("Extra renderer radius. This value is added to the rope radius when rendering. Use it to have visually thicker or thinner ropes.")]
        float extraRendererRadius;
        [SerializeField]
        float segmentLength;
        [SerializeField]
        [Tooltip("Constrain rope segment twisting in relation to each other. Can give more realistic rope behaviour, but will not work well with attached bodies.")]
        bool useTwistLimits;
        [SerializeField]
        [Tooltip("Twist limit from -x to x. A value of 90 will set the twist limit to 180 degrees in total.")]
        float twistLimit;

        /// <summary>
        /// Controls rope stiffness. The lower the value, the stiffer the rope
        /// Too soft will not pull rope together, too stiff send too much waves
        /// compliance = 0.00000000004f; //  0.04 x 10^(-9) (M^2/N) Concrete
        /// compliance = 0.16 x 10^(-9) (M^2/N) Wood - good for ropes
        /// compliance = 0.0000000005f; // 
        /// compliance = 0.000000001f; // 1.0  x 10^(-8) (M^2/N) Leather
        /// compliance = 0.000000002f;   // 0.2  x 10^(-7) (M^2/N) Tendon
        /// compliance = 0.0000001f,     // 1.0  x 10^(-6) (M^2/N) Rubber
        /// compliance = 0.00002f;       // 0.2  x 10^(-3) (M^2/N) Muscle (too soft)
        /// compliance = 0.0001f;        // 1.0  x 10^(-3) (M^2/N) Fat
        /// </summary>
        [SerializeField]
        [Tooltip("Controls the rope elasticity. The lower the value, the stiffer the rope.")]
        float elasticCompliance;
        [SerializeField]
        [Tooltip("Controls the rope bend stiffness. The lower the value, the stiffer the rope.")]
        float bendCompliance;
        [SerializeField]
        [Tooltip("Limit in degrees how far can rope segments bend from each other.")]
        float bendLimit;
        [SerializeField]
        [Tooltip("The mass that a meter of rope will weigh. Each individual segment will have its mass adjusted according to this value and the segment length.")]
        float massPerMeter;
        [SerializeField]
        float drag;
        [SerializeField]
        float angularDrag;
        [SerializeField]
        RigidbodyInterpolation interpolation;
        [SerializeField]
        CollisionDetectionMode collisionDetectionMode;
        [SerializeField]
        PhysicMaterial physicMaterial;
        [SerializeField]
        float linearSpring;
        [SerializeField]
        float linearDamper;
        [SerializeField]
        [Tooltip("How far can two segments go apart before the solver will try force them together. Treat this as a physical limit on how far can the elastic rope stretch. If zero, compliance value is ignored.")]
        float maxSegmentSeparation;
        [SerializeField]
        float slerpSpring;
        [SerializeField]
        [Tooltip("The slerp damper value will be calculated by multiplying the segment mass with this value.")]
        float slerpDampingScale;
        [SerializeField]
        [Tooltip("Angular motion between rope segments")]
        ConfigurableJointMotion angularMotion;

        public float Radius => radius;
        public float RendererRadius => radius + extraRendererRadius;
        public float SegmentLength => segmentLength;
        public bool UseTwistLimits => useTwistLimits;
        public float TwistLimit => twistLimit;

        /// <summary>
        /// Controls rope stiffness. The lower the value, the stiffer the rope
        /// Too soft will not pull rope together, too stiff send too much waves
        /// compliance = 0.00000000004f; //  0.04 x 10^(-9) (M^2/N) Concrete
        /// compliance = 0.16 x 10^(-9) (M^2/N) Wood - good for ropes
        /// compliance = 0.0000000005f; // 
        /// compliance = 0.000000001f; // 1.0  x 10^(-8) (M^2/N) Leather
        /// compliance = 0.000000002f;   // 0.2  x 10^(-7) (M^2/N) Tendon
        /// compliance = 0.0000001f,     // 1.0  x 10^(-6) (M^2/N) Rubber
        /// compliance = 0.00002f;       // 0.2  x 10^(-3) (M^2/N) Muscle (too soft)
        /// compliance = 0.0001f;        // 1.0  x 10^(-3) (M^2/N) Fat
        /// </summary>
        public float ElasticCompliance => elasticCompliance;
        public float BendCompliance => bendCompliance;
        public float BendLimit => bendLimit;
        public float MassPerMeter => massPerMeter;
        public float Drag => drag;
        public float AngularDrag => angularDrag;
        public RigidbodyInterpolation Interpolation => interpolation;
        public CollisionDetectionMode CollisionDetectionMode => collisionDetectionMode;
        public PhysicMaterial PhysicMaterial => physicMaterial;
        public float LinearSpring => linearSpring;
        public float LinearDamper => linearDamper;
        public float MaxSegmentSeparation => maxSegmentSeparation;
        public float SlerpSpring => slerpSpring;
        public float SlerpDampingScale => slerpDampingScale;
        public ConfigurableJointMotion AngularMotion => angularMotion;

        // Return physic material value or unity defaults
        public float StaticFriction => physicMaterial ? physicMaterial.staticFriction : 0.6f;
        public float DynamicFriction => physicMaterial ? physicMaterial.dynamicFriction : 0.6f;

        public static RopeProfileData CreateDefault()
        {
            return new RopeProfileData
            {
                radius = 0.05f,
                extraRendererRadius = 0f,
                segmentLength = 0.4f,
                useTwistLimits = false,
                twistLimit = 15f,
                elasticCompliance = 0.00000000016f,
                bendCompliance = 0.01f,
                bendLimit = 60f,
                massPerMeter = 5f,
                drag = 0,
                angularDrag = 0.05f,
                interpolation = RigidbodyInterpolation.Interpolate,
                collisionDetectionMode = CollisionDetectionMode.Continuous,
                linearSpring = 0f,
                linearDamper = 0f,
                maxSegmentSeparation = 0.25f,
                slerpSpring = 0f,
                slerpDampingScale = 5f,
                angularMotion = ConfigurableJointMotion.Free,
            };
        }

        public RopeProfileData ApplyOverride(RopeProfileOverride over)
        {
            RopeProfileData copy = this;
            copy.segmentLength = over.SegmentLength;
            copy.useTwistLimits = over.UseTwistLimits;
            copy.twistLimit = over.TwistLimit;
            return copy;
        }

        public static string GetComparisonReport(RopeProfileData a, RopeProfileData b)
        {
            var sb = new StringBuilder();

            void AppendErrorString(string name, string first, string second)
            {
                sb.AppendLine($"{name} {first} => {second};");
            }

            void AppendError(string name, float first, float second)
            {
                AppendErrorString(name, first.ToString(), second.ToString());
            }

            void CompareFloats(string name, float first, float second)
            {
                if (first != second)
                {
                    AppendError(name, first, second);
                }
            }

            CompareFloats("Radius", a.radius, b.radius);
            CompareFloats("Segment Length", a.segmentLength, b.segmentLength);
            CompareFloats("Mass Per Meter", a.massPerMeter, b.massPerMeter);
            CompareFloats("Rigidbody Drag", a.drag, b.drag);
            CompareFloats("Rigidbody Angular Drag", a.angularDrag, b.angularDrag);
            if (a.interpolation != b.interpolation)
                AppendErrorString("Rigidbody Interpolation", a.interpolation.ToString(), b.interpolation.ToString());
            if (a.collisionDetectionMode != b.collisionDetectionMode)
                AppendErrorString("Rigidbody Collision Detection", a.collisionDetectionMode.ToString(), b.collisionDetectionMode.ToString());
            if (a.physicMaterial != b.physicMaterial)
                AppendErrorString("Physic Material", $"{a.physicMaterial}", $"{b.physicMaterial}"); // Protect against nulls
            CompareFloats("Joint Linear Spring", a.linearSpring, b.linearSpring);
            CompareFloats("Joint Linear Damper", a.linearDamper, b.linearDamper);
            CompareFloats("Joint Slerp Spring", a.slerpSpring, b.slerpSpring);
            CompareFloats("Joint Slerp Damping Scale", a.slerpDampingScale, b.slerpDampingScale);
            if (a.angularMotion != b.angularMotion)
                AppendErrorString("Joint Angular Motion", a.angularMotion.ToString(), b.angularMotion.ToString());
            if (a.useTwistLimits != b.useTwistLimits)
                AppendErrorString("Joint Twist Limit", a.useTwistLimits.ToString(), b.useTwistLimits.ToString());
            if (b.useTwistLimits)
                CompareFloats("Joint Twist Limit Angle", a.twistLimit, b.twistLimit);

            return sb.ToString();
        }
    }


    [System.Serializable]
    public struct RopeProfileOverride
    {
        [SerializeField]
        float segmentLength;
        public float SegmentLength => segmentLength;

        [SerializeField]
        bool useTwistLimits;
        public bool UseTwistLimits => useTwistLimits;

        [SerializeField]
        float twistLimit;
        public float TwistLimit => twistLimit;

        [SerializeField]
        [HideInInspector]
        internal int serializedVersion;
        const int currentVersion = 1;

        public void InitializeDefaults(RopeProfileData targetData)
        {
            if (serializedVersion < 1)
            {
                segmentLength = targetData.SegmentLength;
                useTwistLimits = targetData.UseTwistLimits;
                twistLimit = targetData.TwistLimit;
            }
            serializedVersion = currentVersion;
        }
    }
}