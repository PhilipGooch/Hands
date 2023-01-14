#define MINIMIZE_FEEDBACK_TORQUE

using NBG.Unsafe;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public struct BallData
    {
        public int bodyId;
        public float radius;
        public float jumpHeight;
        public float speed;

        public float3 acceleration;
        public float3 targetAngularVelocity;

        public float3 slopeDir; // free ball rotation allowed that direction
        public float slope;

        public float3 groundForce;
        
        public float3 groundVelocity;
        public float groundAngularVelocity;
        public float3 groundNormal;
        public float lastGroundHitTimer; // any ground hit time
        public float lastLevelGroundHitTimer; // non sloped ground hit timer

        public float GROUND_TOLERANCE_TIMEOUT; // overridable, not const
        public float timeSinceGround => math.max(0, lastGroundHitTimer - GROUND_TOLERANCE_TIMEOUT);
        public bool grounded => lastGroundHitTimer <= GROUND_TOLERANCE_TIMEOUT;
        



    }

    public class Ball : MonoBehaviour
    {
        public const float kDefaultBallRadius = 0.25f;
        public LayerMask collisionLayers = 1;

        public float speed = 2.5f;
        public float jumpHeight = .6f;


        [NonSerialized] public float radius;
        Rigidbody body;
        SphereCollider ballCollider;

        const float MAX_SLOPE_ANGLE = 50;
        const float MIN_SLOPE_ANGLE = 40;
        const float MAX_CLIMB_STEP = .35f; // step on which should climb effortlessly

        // jump config
        const float JUMP_LIMIT_UP_MOMENTUM = .25f; // remove some of the upward momentum to prevent too high dynamic jumps
        const float JUMP_FORWARD_BOOST = .2f; // speed * multiplier will be added when jumping
        const float JUMP_BOOST_FROM = 1.25f; // full jump boost will be applied when running below this*speed
        const float JUMP_BOOST_TO = 1.5f; // no jump boost applied when running above this*speed
        const float MAX_CENTRIFUGAL_ACCELERATION = 3;

        public const float DEFAULT_GROUNDED_TOLERANCE_TIMEOUT = 0.1f;

        static float scanAbove;
        static float scanAhead;



        float GROUND_TOLERANCE_TIMEOUT => groundToleranceOverride != null ? groundToleranceOverride(this) : DEFAULT_GROUNDED_TOLERANCE_TIMEOUT;
        public Func<Ball, float> groundToleranceOverride = null;

        public CollisionUtils.GroundObject groundObject;
        public List<CollisionUtils.GroundObject> groundObjects = new List<CollisionUtils.GroundObject>();
        float groundObjectsDistSum;


        //public float3 groundForce;
        private float3 _groundImpulse;
        //public float3 groundVelocity;
        //float groundWeight;
        //float3 groundNormal;
        //float lastGroundHitTimer; // any ground hit time
        //float lastLevelGroundHitTimer; // non sloped ground hit timer
        //public float timeSinceGround =>  math.max(0, lastGroundHitTimer - GROUND_TOLERANCE_TIMEOUT);
        //public bool grounded => lastGroundHitTimer <= GROUND_TOLERANCE_TIMEOUT;
        //public bool onSlope => grounded && lastLevelGroundHitTimer > GROUND_TOLERANCE_TIMEOUT;



        public void OnCreate()
        {
            body = GetComponent<Rigidbody>();
            body.inertiaTensor *= 2;

            ballCollider = GetComponentInChildren<SphereCollider>();
            radius = ballCollider.radius * ballCollider.transform.lossyScale.x;

            // step detection sensor position is calculated from ball collision point:
            // - making sure it's above max step height (assuming we hit at ball center height)
            // - but also moving forward enough to hit non-climbable slope
            var tanAllow = math.tan(math.radians(MIN_SLOPE_ANGLE));
            var tanHalfAllow = math.tan(math.radians(MIN_SLOPE_ANGLE / 2));
            var allowAboveHit = MAX_CLIMB_STEP - radius;
            scanAbove = allowAboveHit + radius;
            scanAhead = allowAboveHit / tanAllow - radius * tanHalfAllow;
        }

       

        public static void ProcessGround(ref BallData ball, float3 forward, float moveMagnitude, float3 ragdollVelocity, float3 g, float slope)
        {
            var dt = World.main.dt;
            //Debug.Log(groundImpulse);


            var moveVec = forward * moveMagnitude * ball.speed; // input direction in world coords

            var acceleration = HandleSlopeAndStep(ref ball, forward, g, slope);
            acceleration += HandleCentrifugal(ball);
            acceleration += HandleInputAcceleration(ball, forward, moveMagnitude, ball.speed, ragdollVelocity, dt);

            // write state
            ball.acceleration = acceleration;
            ball.targetAngularVelocity = math.cross(new float3(0, 1, 0), moveVec / ball.radius);
        }

      
      
        private static float3 HandleSlopeAndStep(ref BallData ball, float3 forward, float3 g, float slope)
        {
            float3 acceleration = float3.zero;
            ball.slopeDir = float3.zero;
            if (ball.groundNormal.y > 0)
            {
                // clamp input move vector to block going uphill when sliding
                if (slope >0)
                    ball.slopeDir = math.normalizesafe(ball.groundNormal.ZeroY());

                // prevend slope drift: redirect gravity to press againts sloped ground to prevent sliding
                var adjustedNormal = ball.groundNormal;
                adjustedNormal -= re.ProjectToPositive(adjustedNormal, forward); // ignore directions where player wants to go downhill
                acceleration = (-g - adjustedNormal * math.length(g));
            }
            return acceleration;
        }
        private static float3 HandleCentrifugal(in BallData data)
        {
            //groundAngularVelocity = groundObject.body != null ? groundObject.body.angularVelocity.y : 0;
            return math.cross(re.up * data.groundAngularVelocity, data.groundVelocity).Clamp(MAX_CENTRIFUGAL_ACCELERATION);
        }

        private static float3 HandleInputAcceleration(in BallData ball, float3 forward, float moveMagnitude, float speed, float3 ragdollVelocity, float dt)
        {
            var swingPhase = 0;
            var maxAcc = 2 * moveMagnitude;
            var maxFriction = .5f;

            if (!ball.grounded) speed *= 1f + swingPhase; // in-air more speed
            if (!ball.grounded) maxFriction *= moveMagnitude;// in-air, don't apply friction if not asked
            var groundVel = ragdollVelocity.ZeroY();
            groundVel -= ball.groundVelocity.ZeroY();
            var forwardVel = math.dot(forward, groundVel);

            var frictionDeltaV = -groundVel + forward * math.max(0, forwardVel); // preserve forward momentum
            var frictionAcc = (frictionDeltaV / dt).Clamp(maxFriction);

            var forwardDeltaV = math.max(0, moveMagnitude * speed - forwardVel); // just accelerate forward
            var forwardAcc = math.min(maxAcc, forwardDeltaV / dt);

            return frictionAcc + forward * forwardAcc;
        }

        public void OnFixedUpdate(ref BallData ball, float3 forward, float moveMagnitude, bool ignoreGround)
        {
            var dt = World.main.dt;
            // copy config to be available everywhere
            ball.radius = radius;
            ball.speed = speed;
            ball.jumpHeight = jumpHeight;
            ball.GROUND_TOLERANCE_TIMEOUT = GROUND_TOLERANCE_TIMEOUT;

            ball.lastGroundHitTimer += dt;
            ball.lastLevelGroundHitTimer += dt;
            if (ignoreGround) ball.lastGroundHitTimer = float.MaxValue;

            
            ball.groundForce = _groundImpulse / dt;
            _groundImpulse = float3.zero;

            var moveVec = forward * moveMagnitude;

            var ballPos = World.main.GetBodyPosition(ball.bodyId).pos;
            // look for step ahead
            if (AnalyzeGround(ballPos + moveVec * dt, out ball.groundNormal, out var groundWeight, out groundObjectsDistSum, groundObjects, out var obstacle))
            {
                if (groundWeight < 1 && CouldClimb(obstacle, forward))
                    groundWeight = 1;
            }
            // nothing ahead, check the ground below
            if (groundWeight < 1)
                AnalyzeGround(ballPos + re.down * .1f, out ball.groundNormal, out groundWeight, out groundObjectsDistSum, groundObjects, out _);

            if (groundObjects.Count > 0)
            {
                groundObject = CollisionUtils.FindSupportingObject(groundObjects);
                ball.lastGroundHitTimer = 0;
                if (groundWeight == 1)
                    ball.lastLevelGroundHitTimer = 0;
            }
            else
                groundObject = default;
            var isDynamic = groundObject.body != null && !groundObject.body.isKinematic;
            var groundVelocityMix = isDynamic ? re.InverseLerp(20, 100, groundObject.body.mass) : 1; // only use relative velocity on heavy objects or kinematic objects
            ball.groundVelocity = groundObject.body != null ? (float3)groundVelocityMix * groundObject.body.GetPointVelocity(groundObject.pos) : float3.zero;
            ball.groundAngularVelocity = groundObject.body != null ? groundObject.body.angularVelocity.y : 0;
            ball.slope = 1 - groundWeight;
        }


        private bool AnalyzeGround(float3 scanPos, out float3 groundNormal, out float weight, out float totalDist, List<CollisionUtils.GroundObject> groundObjects, out float3 obstacle)
        {
            if (CollisionUtils.DepenetratePosition(scanPos, radius, transform.root, collisionLayers, out var depenetrated, out totalDist, groundObjects))
            {
                //NoodleDebug.builder.WireCapsule(scanPos, depenetrated, radius, Color.yellow);
                obstacle = depenetrated - math.normalize(depenetrated - scanPos) * radius;
                groundNormal = math.normalize(scanPos - obstacle);
                var angle = math.acos(groundNormal.y);
                weight = re.InverseLerp(MAX_SLOPE_ANGLE, MIN_SLOPE_ANGLE, math.degrees(angle));
                return true;
            }
            else
            {
                weight = 0;
                groundNormal = default;
                obstacle = default;
                return false;
            }
        }

        public bool CouldClimb(Vector3 hitPoint, Vector3 walkForward)
        {
            var scanPos = hitPoint + Vector3.up * scanAbove + walkForward * scanAhead;
            // DebugExtension.DebugWireSphere(scanPos, Color.black, radius);


            // check if the place above obstacle is free to climb on top
            if (CollisionUtils.CheckSphere(scanPos, radius, collisionLayers, transform.root))
            {
                //NoodleDebug.builder.WireSphere(scanPos, radius, Color.red);
                return false;
            }

            // return true if unknown
            //NoodleDebug.builder.WireSphere(scanPos, radius, Color.green);

            return true;
        }


       


        public static float3 Jump(BallData data, float3 forward, float tonus, float3 ragdollVelocity, float3 g, float dt)
        {
            // x= g*t^2/2;
            //t = Sqrt(x/g)
            var t = math.sqrt(2 * data.jumpHeight * tonus / math.length(g));
            var upVelocityChange = -t * g;

            // stop from falling down
            if (ragdollVelocity.y < 0)
                upVelocityChange -= re.up * ragdollVelocity.y;
            else
                upVelocityChange -= JUMP_LIMIT_UP_MOMENTUM * re.up * ragdollVelocity.y; // prevent jumping too high up
            var acceleration = upVelocityChange / dt;

            //var curAcc = acceleration;
            //if (curAcc.y > 0)
            //    acceleration.y -= curAcc.y;

            var forwardSpeed = math.dot(forward, ragdollVelocity);
            var multiplier = re.InverseLerp(data.speed * JUMP_BOOST_TO, data.speed * JUMP_BOOST_FROM, forwardSpeed); // don't add forward impulse when traveling at 1.5x
            var forwardVelocityChange = JUMP_FORWARD_BOOST * data.speed * multiplier;
            acceleration += forwardVelocityChange * forward / dt;

            return acceleration;
        }
        unsafe struct Weights
        {
            public float4 w0;
            public float4 w1;
            public float4 w2;
            public float4 w3;
        }

        public static unsafe void ApplyBallAcceleration(BallData ball, int* links, int nLinks, float slope)
        {
            
            var world = World.main;
            
            if (slope>0)// math.lengthsq( ball.slopeDir)!=0)
            {
                ball.acceleration -= re.ProjectToPositive(ball.acceleration, -ball.slopeDir)*slope;
                var slopeCross =  math.cross(new float3(0, 1, 0), ball.slopeDir);
                ball.targetAngularVelocity -= re.ProjectToPositive(ball.targetAngularVelocity, -slopeCross) * slope; // remove force going upslope
                ball.targetAngularVelocity += re.ProjectToPositive(world.GetVelocity4(ball.bodyId).angular3, slopeCross) * slope; // preserve velocity going downslope
                //moveVec = re.ProjectOnPlane(moveVec, slopeDir) + slopeDir * math.max(0, math.dot(moveVec, slopeDir));
                //ball.acceleration *= .1f+ballDrive*.9f;
                //ball.targetAngularVelocity *= ballDrive;
            }

            //Debug.Log($"{ball.groundWeight:F2} {ball.targetAngularVelocity} {world.GetVelocity4(ball.bodyId).angular3} ");
            world.GetVelocity4(ball.bodyId).angular3 = ball.targetAngularVelocity;

            // apply acceleration to ragdoll, but using weighted influences
            var weights = new Weights() { w0 = new float4(.5f, 1, .9f, .8f) }; // ball, hips, waist, chest
            var w = (float*)weights.AsPointer();
            var totalMass = 0f;
            var weightedMass = 0f;
            for (int b = 0; b < nLinks; b++)
            {
                var m = world.GetBody(links[b]).m;
                totalMass += m;
                weightedMass += w[b] * m;
            }
            for (int b = 0; b < nLinks; b++)
            {
                //var m = world.GetBody(links[b]).m;
                world.AddLinearVelocity(links[b], w[b]*totalMass/weightedMass* ball.acceleration * world.dt);
            }
        }
        public void DistributeAccelerationToGround(float3 acceleration, float ragdollMass, float dt)
        {
            for (int i = 0; i < groundObjects.Count; i++)
                if (groundObjects[i].body != null && !groundObjects[i].body.isKinematic)
                {

#if MINIMIZE_FEEDBACK_TORQUE
                    var groundPos = groundObjects[i].pos;
                    // minimize torque by moving force point closer to center of the object, but no more than radius 
                    groundPos = re.MoveTowards(groundPos, math.lerp(groundPos, groundObjects[i].body.worldCenterOfMass, .75f), 1 * radius);
                    groundObjects[i].body.AddForceAtPosition(-ragdollMass * acceleration * groundObjects[i].dist / groundObjectsDistSum, groundPos);

#else
                groundObjects[i].body.AddForceAtPosition(-ragdollMass * acceleration * groundObjects[i].dist / groundObjectsDistSum, groundObjects[i].pos);
#endif
                }
        }

        private void OnCollisionStay(Collision collision)
        {
            //for(int i=0;i<)
            _groundImpulse += (float3)collision.impulse;
        }

    }
}