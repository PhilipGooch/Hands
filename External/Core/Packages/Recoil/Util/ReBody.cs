using UnityEngine;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Recoil
{
    public struct ReBody
    {
        Rigidbody rig;
        int id;
        bool bodyExists;

        /// <summary>
        /// Constructs a new ReBody from the target Rigidbody.
        /// Tries to grab the recoil id of the target.
        /// </summary>
        /// <param name="target">A rigidbody.</param>
        public ReBody(Rigidbody target)
        {
            if (target != null)
            {
                rig = target;
                id = ManagedWorld.main.FindBody(rig, true);
                bodyExists = true;
            }
            else
            {
                rig = null;
                id = World.environmentId;
                bodyExists = false;
            }
        }

        /// <summary>
        /// Creates an empty ReBody.
        /// Useful for clearing a ReBody field.
        /// </summary>
        /// <returns>An empty ReBody.</returns>
        public static ReBody Empty()
        {
            return new ReBody
            {
                id = World.environmentId,
                bodyExists = false
            };
        }


        public bool Equals(ReBody other)
        {
            return other.rig == rig;
        }

        public override bool Equals(object obj)
        {
            if (obj is ReBody other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return rig.GetHashCode();
        }

        public static bool operator ==(ReBody lhs, ReBody rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(ReBody lhs, ReBody rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// Was this ReBody initialized with a rigidbody.
        /// </summary>
        public bool BodyExists => bodyExists;

        /// <summary>
        /// Called only when we need to do something in recoil. If a body still doesn't have an id, we're going to run into problems.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void EnsureId()
        {
            if (id == World.environmentId)
            {
                id = ManagedWorld.main.FindBody(rig);
            }
        }

        /// <summary>
        /// Get the rigidbody of this ReBody.
        /// </summary>
        public Rigidbody rigidbody => rig;

        /// <summary>
        /// Get the Recoil Id of this ReBody.
        /// </summary>
        public int Id
        {
            get
            {
                EnsureId();
                return id;
            }
        }

        /// <summary>
        /// Get or set the Recoil velocity of this body. More efficient than setting linear/angular velocities separately.
        /// </summary>
        public MotionVector reVelocity
        {
            get
            {
                EnsureId();
                return World.main.GetVelocity(id);
            }
            set
            {
                EnsureId();
                World.main.SetVelocity(id, value);
            }
        }

        /// <summary>
        /// Get or set the linear velocity of this body.
        /// </summary>
        public Vector3 velocity
        {
            get
            {
                return reVelocity.linear;
            }
            set
            {
                var tmp = reVelocity;
                tmp.linear = value;
                reVelocity = tmp;
            }
        }

        /// <summary>
        /// Get or set the angular velocity of this body.
        /// </summary>
        public Vector3 angularVelocity
        {
            get
            {
                return reVelocity.angular;
            }
            set
            {
                var tmp = reVelocity;
                tmp.angular = value;
                reVelocity = tmp;
            }
        }

        /// <summary>
        /// Get or set the Recoil transform of this body. More efficient than setting position and rotation separately.
        /// </summary>
        public RigidTransform rePosition
        {
            get
            {
                EnsureId();
                return World.main.GetTransformPosition(id);
            }
            set
            {
                EnsureId();
                World.main.SetTransformPosition(id, value);
            }
        }

        /// <summary>
        /// Get or set the position of this body.
        /// </summary>
        public Vector3 position
        {
            get
            {
                return rePosition.pos;
            }
            set
            {
                var tmp = rePosition;
                tmp.pos = value;
                rePosition = tmp;
            }
        }

        /// <summary>
        /// Get or set the rotation of this body.
        /// </summary>
        public Quaternion rotation
        {
            get
            {
                return rePosition.rot;
            }
            set
            {
                var tmp = rePosition;
                tmp.rot = value;
                rePosition = tmp;
            }
        }

        /// <summary>
        /// Get the Recoil Body instance.
        /// </summary>
        ref Body GetBody()
        {
            EnsureId();
            return ref World.main.GetBody(id);
        }

        /// <summary>
        /// Get the world center of mass of this body.
        /// </summary>
        public Vector3 worldCenterOfMass => GetBody().x.pos;

        /// <summary>
        /// Get or set the center of mass for this body.
        /// </summary>
        public Vector3 centerOfMass
        {
            get
            {
                return InverseTransformPoint(worldCenterOfMass);
            }
            set
            {
                EnsureId();
                rig.centerOfMass = value;
                ManagedWorld.main.ResyncPhysXBody(rig);
            }
        }

        /// <summary>
        /// Does this body use gravity?
        /// </summary>
        public bool useGravity
        {
            get
            {
                return rig.useGravity;
            }
            set
            {
                rig.useGravity = value;
            }
        }

        /// <summary>
        /// Is this body kinematic?
        /// </summary>
        public bool isKinematic
        {
            get
            {
                EnsureId();
                return World.main.PhysXKinematicState(id);
            }
        }

        /// <summary>
        /// Get or set the mass of this body.
        /// </summary>
        public float mass
        {
            get
            {
                return GetBody().m;
            }
            set
            {
                rig.mass = value;
                ManagedWorld.main.ResyncPhysXBody(rig);
            }
        }

        /// <summary>
        /// Get the inverse mass of this body.
        /// </summary>
        public float invMass => GetBody().invM;

        /// <summary>
        /// Get or set the linear drag of the body.
        /// </summary>
        public float drag
        {
            get
            {
                return rig.drag;
            }
            set
            {
                rig.drag = value;
            }
        }

        /// <summary>
        /// Get or set the angular drag of the body.
        /// </summary>
        public float angularDrag
        {
            get
            {
                return rig.angularDrag;
            }
            set
            {
                rig.angularDrag = value;
            }
        }

        /// <summary>
        /// Get or set the max angular velocity of the body.
        /// </summary>
        public float maxAngularVelocity
        {
            get
            {
                return rig.maxAngularVelocity;
            }
            set
            {
                rig.maxAngularVelocity = value;
            }
        }

        /// <summary>
        /// Get or set the inertia tensor of this body.
        /// </summary>
        public Vector3 inertiaTensor
        {
            get
            {
                return rig.inertiaTensor;
            }
            set
            {
                rig.inertiaTensor = value;
                ManagedWorld.main.ResyncPhysXBody(rig);
            }
        }

        /// <summary>
        /// Get the inertia tensor rotation of this body.
        /// </summary>
        public Quaternion inertiaTensorRotation => rig.inertiaTensorRotation;

        /// <summary>
        /// Get or set Recoil sleeping for this body. Recoil automatically tries to sleep bodies which are not moving.
        /// This property can be used to prevent Recoil sleeping.
        /// </summary>
        public bool AllowSleeping
        {
            get
            {
                EnsureId();
                return World.main.GetIsSleepAllowed(id);
            }
            set
            {
                EnsureId();
                World.main.ConfigureBodySleep(id, value);
            }
        }

        /// <summary>
        /// Get Recoil inverse inertia for this body.
        /// </summary>
        public Recoil.lt3x3 invI => GetBody().invI;

        /// <summary>
        /// Transform a local point to world space for this body. Uses transform position.
        /// </summary>
        public Vector3 TransformPoint(Vector3 localPoint)
        {
            EnsureId();
            // World.main.TransformPoint uses center of mass instead of transform position
            var x = World.main.GetTransformPosition(id);
            return math.transform(x, localPoint);
        }

        /// <summary>
        /// Transform a world space point into a local point for this body. Uses transform position.
        /// </summary>
        public Vector3 InverseTransformPoint(Vector3 worldPoint)
        {
            EnsureId();
            // World.main.InverseTransformPoint uses center of mass instead of transform position
            var x = World.main.GetTransformPosition(id);
            return math.transform(math.inverse(x), worldPoint);
        }

        /// <summary>
        /// Transform a local direction to a world space direction for this body.
        /// </summary>
        public Vector3 TransformDirection(Vector3 localDirection)
        {
            EnsureId();
            return World.main.TransformDirection(id, localDirection);
        }

        /// <summary>
        /// Transform a world space direction into a local direction for this body.
        /// </summary>
        public Vector3 InverseTransformDirection(Vector3 worldDirection)
        {
            EnsureId();
            return World.main.InverseTransformDirection(id, worldDirection);
        }

        /// <summary>
        /// Add force for this body. Internally this will calculate and apply a velocity change.
        /// </summary>
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            EnsureId();
            // Entering low level land since mass is not exposed
            var body = World.main.GetBody(id);

            // Apply force over time
            if (mode == ForceMode.Force || mode == ForceMode.Acceleration)
            {
                force *= World.main.dt;
            }

            var velocity = new MotionVector(float3.zero, force);

            // Account for mass
            if (mode == ForceMode.Force || mode == ForceMode.Impulse)
            {
                velocity.linear = body.invM * velocity.linear;
            }

            World.main.AddVelocity(id, velocity);
        }

        /// <summary>
        /// Add relative force for this body. Internally this will calculate and apply a velocity change.
        /// </summary>
        public void AddRelativeForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            var worldForce = TransformDirection(force);
            AddForce(worldForce, mode);
        }

        /// <summary>
        /// Add torque for this body. Internally this will calculate and apply an angular velocity change.
        /// </summary>
        public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            EnsureId();
            // Entering low level land since mass is not exposed
            var body = World.main.GetBody(id);

            // Apply force over time
            if (mode == ForceMode.Force || mode == ForceMode.Acceleration)
            {
                torque *= World.main.dt;
            }

            var velocity = new MotionVector(torque, float3.zero);

            // Account for mass
            if (mode == ForceMode.Force || mode == ForceMode.Impulse)
            {
                velocity.angular = re.mul(body.invI, velocity.angular);
            }

            World.main.AddVelocity(id, velocity);
        }

        /// <summary>
        /// Add relative torque for this body. Internally this will calculate and apply an angular velocity change.
        /// </summary>
        public void AddRelativeTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            var worldTorque = TransformDirection(torque);
            AddTorque(worldTorque, mode);
        }

        // World.main has ApplyImpulse but it's only an impulse and does not implement any other force mode
        // Therefore we implement everything ourselves
        /// <summary>
        /// Add force at position for this body. Internally this will calculate and apply a velocity change.
        /// </summary>
        public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force)
        {
            EnsureId();
            // Entering low level land since mass is not exposed
            var body = World.main.GetBody(id);
            var relativePoint = (float3)position - body.x.pos;

            // Apply force over time
            if (mode == ForceMode.Force || mode == ForceMode.Acceleration)
            {
                force *= World.main.dt;
            }

            var velocity = new MotionVector(math.cross(relativePoint, force), force);

            // Account for mass
            if (mode == ForceMode.Force || mode == ForceMode.Impulse)
            {
                velocity.angular = re.mul(body.invI, velocity.angular);
                velocity.linear = body.invM * velocity.linear;
            }

            World.main.AddVelocity(id, velocity);
        }

        /// <summary>
        /// Reset the inertia tensor for this body.
        /// </summary>
        public void ResetInertiaTensor()
        {
            EnsureId();
            rig.ResetInertiaTensor();
            ManagedWorld.main.ResyncPhysXBody(rig);
        }

        /// <summary>
        /// Add explosion force for this body. Internally this will calculate and apply a velocity change.
        /// </summary>
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier = 0f, ForceMode mode = ForceMode.Force)
        {
            EnsureId();

            // This might not be good enough. If that's the case then we could detect the closest point of all colliders.
            var closestPoint = rig.ClosestPointOnBounds(explosionPosition);
            var delta = closestPoint - explosionPosition;
            var distance = delta.magnitude;
            var forceStrength = Mathf.InverseLerp(explosionRadius, 0f, distance);
            // Not sure if this is correct since the manual is a bit vague
            var direction = Vector3.Lerp(delta.normalized, Vector3.up, upwardsModifier);
            var finalForce = direction * explosionForce * forceStrength;
            AddForceAtPosition(finalForce, closestPoint, mode);
        }

        /// <summary>
        /// Get the velocity of a world space point for this body.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 GetPointVelocity(Vector3 point)
        {
            EnsureId();
            return World.main.GetWorldPointVelocity(id, point).linear;
        }

        /// <summary>
        /// Wake this body from Physx sleep.
        /// </summary>
        public void WakeUp()
        {
            rig.WakeUp();
        }

        /// <summary>
        /// Teleport this body to a new position and rotation.
        /// </summary>
        public void SetBodyPlacementImmediate(Vector3 position, Quaternion rotation)
        {
            EnsureId();
            ManagedWorld.main.SetBodyPlacementImmediate(id, new RigidTransform(rotation, position));
        }
    }
}
