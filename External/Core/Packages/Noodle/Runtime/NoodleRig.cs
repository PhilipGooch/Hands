using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using NBG.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using NBG.Unsafe;

namespace Noodles
{
    public struct ArticulationRef
    {
        public int articulationId;
    }
    public class NoodleRig : MonoBehaviour, IGetBody, IHandlesRigidbodies
    {
        public const bool HAND_FEEDBACK_TO_CHEST_ANCHOR = false; // where feeback froce from handIK is applied: true - more stable, false - more physical

        [SerializeField] private Rigidbody[] chain;
        [SerializeField] private NoodleSprings springs;
        private ConfigurableJoint shoulderL;
        private ConfigurableJoint shoulderR;
        [NonSerialized]


        int articulationId;
        int[] links;
        public int count => links.Length;
        public ref Body GetBody(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref World.main.GetBody(links[link]);
        }
        public ref Velocity4 GetVelocity4(int link)
        {
            Unsafe.CheckIndex(link, count);
            return ref World.main.GetVelocity4(links[link]);
        }
        public int GetBodyId(int link) => links[link];

        public ref Articulation GetArticulation() => ref World.main.GetArticulation(articulationId);
        public float3 ballPosition => GetBody(NoodleBones.Ball).x.pos;
        public float3 rootPosition => GetBody(NoodleBones.Ball).x.pos-new float3(0,dimensions.ballRadius,0);
        public float3 fullCenterOfMass => this.CalculateCenterOfMass(0, 13);
        public float3 fullVelocity => this.CalculateVelocity(0,13);
        float3 coreCenterOfMass => this.CalculateCenterOfMass(NoodleBones.Hips, 3);
        float3 coreVelocity => this.CalculateVelocity(NoodleBones.Hips, 3);

        public float3 position => fullCenterOfMass.SetY(coreCenterOfMass.y); // horizontal position y is calculated just from core
        public float3 velocity => fullVelocity.SetY(coreVelocity.y);


        public NoodleDimensions dimensions;
        public float mass => dimensions.suspendedMass;

        Ball ball;

        public Entity entity;

        public int ballId => links[NoodleBones.Ball];
        public int chestId => links[NoodleBones.Chest];

        bool _rigidbodiesRegistered;
        void IHandlesRigidbodies.OnRegisterRigidbodies()
        {
            if (_rigidbodiesRegistered)
                return;
            _rigidbodiesRegistered = true;

            foreach (var b in chain)
            {
                ManagedWorld.main.RegisterBody(b);
                b.maxAngularVelocity = 120;
            }
        }

        void IHandlesRigidbodies.OnUnregisterRigidbodies()
        {
            if (!_rigidbodiesRegistered)
                return;
            _rigidbodiesRegistered = false;

            foreach (var b in chain)
            {
                ManagedWorld.main.UnregisterBody(b);
            }
        }

        public bool IsAlive { get; private set; }

        static ComponentTypeList types;
        public unsafe void OnCreate(Entity entity, float3 ikAnchorL, float3 ikAnchorR, float3 footAnchorL, float3 footAnchorR)
        {
            this.entity = entity;
            
            if (types.Length == 0)
            {
                types = ComponentTypeList.Create();
                types.AddType<ArticulationRef>();
                types.AddType<BallData>();
                types.AddType<NoodleSuspensionData>();
                types.AddType<NoodleSprings>();
                types.AddType<NoodleDimensions>();
            }
            EntityStore.AddComponents(entity, types);
            ((IHandlesRigidbodies)this).OnRegisterRigidbodies();
            if (!ball)
                ball = GetComponentInChildren<Ball>();
            ball.OnCreate();
            EntityStore.AddComponentObject(entity, ball);
            var entityRef = EntityStore.GetEntityReference(entity);

            var structure = ArticulationReader.ReadStructure(chain);
            
            var suspension = new NoodleSuspensionData()
            {
                ballLink = NoodleBones.Ball,
                link = NoodleBones.Hips,
                anchor = -(float3)chain[NoodleBones.Hips].TransformDirection(chain[NoodleBones.Hips].centerOfMass), // place anchor at transform origin
                axis = re.up,
            };
            entityRef.GetComponentData<NoodleSuspensionData>() = suspension;
            dimensions = NoodleJoints.Measure(structure, suspension, ball.radius, ikAnchorL, ikAnchorR, footAnchorL, footAnchorR);
            entityRef.GetComponentData<NoodleDimensions>() = dimensions;

            var joints = NoodleJoints.CreateJoints(structure, dimensions);


            
            ref var articulation = ref ManagedWorld.main.AddArticulation(out articulationId);
            ref var characterArticulation = ref entityRef.GetComponentData<ArticulationRef>();
            articulation.Allocate(articulationId, chain, joints);
            characterArticulation.articulationId = articulationId;

            entityRef.GetComponentData<BallData>().bodyId = articulation.solver.links[NoodleBones.Ball];
            IgnoreSelfCollisions(chain,transform);
           
            // store copy for quick access
            links = new int[articulation.solver.nLinks];
            for (int i = 0; i < links.Length; i++)
                links[i] = articulation.solver.links[i];

            shoulderL = chain[NoodleBones.UpperArmL].GetComponent<ConfigurableJoint>();
            shoulderR = chain[NoodleBones.UpperArmR].GetComponent<ConfigurableJoint>();

            IsAlive = true;
        }

        public void Dispose()
        {
            IsAlive = false;

            ((IHandlesRigidbodies)this).OnUnregisterRigidbodies();
            ManagedWorld.main.RemoveArticulation(articulationId);
        }

        public static void IgnoreSelfCollisions(Rigidbody[] chain, Transform transform)
        {
            var colliders = transform.GetComponentsInChildren<Collider>();
            foreach (var c1 in colliders)
            {
                //foreach (var c2 in colliders)
                var c2 = chain[NoodleBones.Ball].GetComponentInChildren<Collider>();
                Physics.IgnoreCollision(c1, c2);
            }
            //Physics.IgnoreCollision(chain[NoodleBones.Hips].GetComponentInChildren<Collider>(), chain[NoodleBones.UpperArmL].GetComponentInChildren<Collider>());
            //Physics.IgnoreCollision(chain[NoodleBones.Hips].GetComponentInChildren<Collider>(), chain[NoodleBones.UpperArmR].GetComponentInChildren<Collider>());
            //Physics.IgnoreCollision(chain[NoodleBones.Waist].GetComponentInChildren<Collider>(), chain[NoodleBones.UpperArmL].GetComponentInChildren<Collider>());
            //Physics.IgnoreCollision(chain[NoodleBones.Waist].GetComponentInChildren<Collider>(), chain[NoodleBones.UpperArmR].GetComponentInChildren<Collider>());
        }

        public void OnFixedUpdate(ref BallData ballData, float3 forward, float moveMagnitude, bool ignoreGround)
        {
            Profiler.BeginSample("ProcessBall");
            ball.OnFixedUpdate(ref ballData, forward, moveMagnitude, ignoreGround);
            Profiler.EndSample();

            var entityRef = EntityStore.GetEntityReference(entity);
            entityRef.GetComponentData<NoodleSprings>() = springs;

            CalculateShoulderForces(entityRef);

        }

        private void CalculateShoulderForces(EntityReference entityRef)
        {
            ref var noodle = ref entityRef.GetComponentData<NoodleData>();


            var unityShoulderForceL = shoulderL.currentForce;
            var unityShoulderForceR= shoulderR.currentForce;

            var solver = GetArticulation().solver;
            var posL = Carry.GetWorldAnchor(solver, NoodleJoints.IDX_UpperArmL + 1);
            var impulseL = Carry.GetImpulseAtPos(solver, NoodleJoints.IDX_UpperArmL + 0, posL)
                + Carry.GetImpulseAtPos(solver, NoodleJoints.IDX_UpperArmL + 1, posL)
                + Carry.GetImpulseAtPos(solver, NoodleJoints.IDX_IK + 0, posL);
            noodle.shoulderForceL = ForceVector.Linear(unityShoulderForceL) + impulseL * (1 / World.main.dt);

            var posR = Carry. GetWorldAnchor(solver, NoodleJoints.IDX_UpperArmR + 1);
            var impulseR = Carry.GetImpulseAtPos(solver, NoodleJoints.IDX_UpperArmR + 0, posR)
                + Carry.GetImpulseAtPos(solver, NoodleJoints.IDX_UpperArmR + 1, posR)
                + Carry.GetImpulseAtPos(solver, NoodleJoints.IDX_IK + 1, posR);
            noodle.shoulderForceR = ForceVector.Linear(unityShoulderForceR) + impulseR * (1 / World.main.dt); 
        }

      
        public static void ProcessGround(ref BallData ballData, float3 forward, float moveMagnitude, float3 velocity,float3 gravity, float slope)
        {
            Ball.ProcessGround(ref ballData,forward, moveMagnitude, velocity, gravity, slope);
        }


       

        public void DistributeGroundFeedback()
        {
            var dt = Time.fixedDeltaTime;
            Profiler.BeginSample("DistributeFeedback");
            // feedback
            ball.DistributeAccelerationToGround(EntityStore.GetComponentData<BallData>(entity).acceleration, dimensions.suspendedMass, dt);
            Profiler.EndSample();
        }
        static void ApplyHeadPose(in SolverBodies bodies, in HeadPose pose, in NoodleTorsoJoints n, float aimYaw)
        {
            var chestRot = bodies.GetBody(n.head.connectedLink).x.rot;
            var headRot = pose.rotation;
            headRot = math.mul(math.slerp(quaternion.identity, chestRot, pose.fkParent), headRot);
            headRot = math.mul(quaternion.RotateY(aimYaw), headRot);
            n.head.targetRotation = headRot;
            n.head.relativeVelInfluence = pose.fkParent;
        }
        static void ApplyTorsoPose(in TorsoPose pose, in NoodleTorsoJoints n, float aimYaw)
        {
            n.angularHips.targetRotation = math.mul(quaternion.RotateY(aimYaw), pose.hipsRotation);
            n.angularChest.targetRotation = math.mul(quaternion.RotateY(aimYaw), pose.chestRotation);
            n.waist.targetRotation = re.invmul(pose.hipsRotation, pose.waistRotation);
            n.chest.targetRotation = re.invmul(pose.waistRotation, pose.chestRotation);
        }
        private static void ApplyTorsoSprings(NoodleSprings springs, TorsoPose pose, in NoodleTorsoJoints n, bool grounded, bool swing)
        {
            n.angularHips.springX = n.angularHips.springY = n.angularHips.springZ = springs.angular * pose.angularTonus *(grounded?1:0);
            n.angularChest.springX = springs.angular * pose.angularTonus * (grounded ? 0 : .5f) * (swing?1:5);
            n.angularChest.springY = springs.angular * pose.angularTonus * (grounded ? 0 : .5f);
            n.angularChest.springZ = springs.angular * pose.angularTonus * (grounded ? 0 : .5f);

            n.waist.spring = springs.waist * pose.tonus;// * (swing ? 0.1f:1);
            n.chest.spring = springs.chest * pose.tonus;// * (swing ? 0.1f:1);
        }

        private static void ApplyHeadSprings(NoodleSprings springs, HeadPose pose, in NoodleTorsoJoints n)
        {
            n.head.spring = springs.head * pose.tonus;
        }
        public unsafe static void ApplyPose(NoodleSprings springs, in Articulation articulation, in NoodleDimensions dim, ref NoodleSuspensionData suspension, NoodlePose pose, float aimYaw, bool swing, float moveMag)
        {
            // normalize suspension tonus to avoid degenerate matrices
            if (pose.torso.suspensionTonus < .01f) pose.torso.suspensionTonus = 0;
            else if (pose.torso.suspensionTonus > .99f) pose.torso.suspensionTonus = 1;

            var bodies = articulation.GetBodies();
            var n = new NoodleJoints(articulation.solver.joints);

            n.cg.targetPosition = pose.torso.cg.RotateY(aimYaw).SetY(0);
            suspension.targetPosition = pose.torso.cg.y - dim.ballRadius;
            suspension.tonus = pose.torso.suspensionTonus;
            
            ApplyHeadPose(bodies, pose.head, n.torso, aimYaw);
            ApplyTorsoPose(pose.torso, n.torso, aimYaw);
            var ballOffsetFromRoot = pose.torso.cg.SetY(dim.ballRadius).RotateY(aimYaw);

            NoodleArmRig.ApplyArmPose(bodies, pose.handL, n.armL, dim.armL, aimYaw, ballOffsetFromRoot, true);
            NoodleArmRig.ApplyArmPose(bodies, pose.handR, n.armR, dim.armR, aimYaw, ballOffsetFromRoot, false);
            NoodleLegRig.ApplyLegPose(bodies, pose.legL, n.legL, dim.legL, aimYaw, ballOffsetFromRoot, true);
            NoodleLegRig.ApplyLegPose(bodies, pose.legR, n.legR, dim.legR, aimYaw, ballOffsetFromRoot, false);

            ApplyHeadSprings(springs, pose.head, n.torso);
            ApplyTorsoSprings(springs, pose.torso, n.torso, pose.grounded, swing);
            NoodleArmRig.ApplyArmSprings(springs, pose.handL, n.armL);
            NoodleArmRig.ApplyArmSprings(springs, pose.handR, n.armR);
            NoodleLegRig.ApplyLegSprings(springs, pose.legL, n.legL);
            NoodleLegRig.ApplyLegSprings(springs, pose.legR, n.legR);

            // experiment converting hand pose to relative
            float relativeAmount = 0;
            n.armL.upper.relativeVelInfluence = n.armR.upper.relativeVelInfluence = relativeAmount;
            var toWorld = math.mul(bodies.GetBody(NoodleBones.Chest).x.rot, quaternion.RotateY(-aimYaw));
            n.armL.upper.targetRotation = math.mul(math.slerp(quaternion.identity, toWorld, relativeAmount), n.armL.upper.targetRotation);
            n.armR.upper.targetRotation = math.mul(math.slerp(quaternion.identity, toWorld, relativeAmount), n.armR.upper.targetRotation);


            n.cg.spring = springs.cg;
            // when suspension is relaxed pull ball to center of mass using overdamped spring
            n.cg.springY = new Spring(springs.cg.kp * .1f, springs.cg.kd);
            n.cg.springY *= pose.grounded ? 0:(1 -pose.torso.suspensionTonus);  // don't do if ball touches ground
            //n.cg.springY = new Spring(springs.cg.kp*.1f,springs.cg.kd) * (1 - pose.torso.suspensionTonus); // when suspension is relaxed pull ball to center of mass

            // cg distribution hips a bit heavier
            // when no suspension - tie ball to waist/chest
            var cgBlend = pose.torso.suspensionTonus;
            n.cg.weights0 = math.lerp(new float4(0, 1, 1, 0), 1, cgBlend);
            n.cg.weights1 = math.lerp(0, math.pow(new float4( pose.handL.muscle.upperTonus, pose.handL.muscle.lowerTonus, pose.handR.muscle.upperTonus, pose.handR.muscle.lowerTonus),2), cgBlend);
            n.cg.weights2 = math.lerp(0, math.pow(new float4(pose.legL.upperTonus, pose.legL.lowerTonus, pose.legR.upperTonus, pose.legR.lowerTonus), 2), cgBlend);
            //n.cg.weights1 = math.lerp(0, 1, pose.torso.suspensionTonus);
            //n.cg.weights2 = math.lerp(0, 1, pose.torso.suspensionTonus);

            n.preserveAngular.spring = pose.grounded ? springs.preserveAngular : Spring.free; // angular momentum
        }



        // leg rig: x - right, y-down, z-back
        public static unsafe void ApplyVelocities(ref BallData ball, in CarryData carry, in NoodlePose pose, in Articulation articulation, float3 forward, bool jump, float3 gravity, bool freeFall, float slope)
        {
            if (jump)
            {
                var velocity = articulation.GetBodies().CalculateVelocity(0,13);
                ball.acceleration += Ball.Jump(ball, forward, 1, velocity, World.main.gravity, World.main.dt);
            }
            Ball.ApplyBallAcceleration(ball, articulation.solver.links, articulation.solver.nLinks, slope);

            //HandGravityBias(articulation, worldBodies, worldV, NoodleBones.UpperArmL, NoodleBones.LowerArmL, NoodleBones.Hips, carry.l.state == HandState.Idle ? 0 : 1, gravity, dt);
            //HandGravityBias(articulation, worldBodies, worldV, NoodleBones.UpperArmR, NoodleBones.LowerArmR, NoodleBones.Hips, carry.r.state == HandState.Idle ? 0 : 1, gravity, dt);

            if (!freeFall)
            {
                HandGravityBias(articulation, NoodleBones.UpperArmL, NoodleBones.LowerArmL, NoodleBones.Hips, pose.handL.muscle, gravity);
                HandGravityBias(articulation, NoodleBones.UpperArmR, NoodleBones.LowerArmR, NoodleBones.Hips, pose.handR.muscle, gravity);
            }
        }

        
        private static unsafe void HandGravityBias(in Articulation articulation,  int upperBody, int lowerBody, int feedbackBody, HandMuscle muscle, float3 gravity)
        {
            var world = World.main;
            var links = articulation.solver.links;
            var upperId = links[upperBody];// upperLeft
            var lowerId = links[lowerBody];// upperLeft
            var chestId = links[feedbackBody];// chest

            ref var upperB = ref world.GetBody(upperId);
            ref var lowerB = ref world.GetBody(lowerId);
            var impU = -upperB.m * gravity * world.dt * muscle.upperTonus * muscle.upperTonus;
            var impL = -lowerB.m * gravity * world.dt * muscle.lowerTonus * muscle.lowerTonus;

            //world.GetVelocity4(upperId).linear += new float4(upperB.invM * impU, 0);
            //world.GetVelocity4(lowerId).linear += new float4(lowerB.invM * impL, 0);
            world.ApplyImpulse(upperId, impU);
            world.ApplyImpulse(upperId, impL);

            //// ALT1: just linear feedback, introduces torque
            //worldV.ItemAsRef(chestId * 2 + 1) -= new float4(worldBodies[chestId].invM * (impU + impL), 0);
            //ALT2: full feedback
            world.ApplyImpulseAtWorldPos(chestId, -impU, upperB.x.pos);
            world.ApplyImpulseAtWorldPos(chestId, -impL, lowerB.x.pos);

        }
        
        /// <summary>
        /// Moves the rig (bone structure) instantly by the given offset
        /// This affects all bones and will not interfere with IK.
        /// If you want to teleport a H2Player, please use H2Player::Move, this is only for the actual ragdoll
        /// Safe to call at any point in time, uses ManagedWorld::SetBodyPlacementImmediate(...) internally
        /// </summary>
        /// <param name="offset">delta that is applied to all rigidBodies</param>
        public unsafe void Teleport(float3 offset)
        {
            var articulation = GetArticulation();
            for (int i = 0; i < articulation.solver.nLinks; i++)
            {
                var bodyID = articulation.solver.links[i];
                var body = World.main.GetBody(bodyID);
                var bodyTransform = body.x;
                bodyTransform.pos += offset;
                ManagedWorld.main.SetBodyPlacementImmediate(bodyID, bodyTransform);
            }
        }
        public unsafe void AddLinearVelocity(float3 deltaV)
        {
            var articulation = GetArticulation();
            for (int i = 0; i < articulation.solver.nLinks; i++)
                ManagedWorld.main.SetVelocity(articulation.solver.links[i], World.main.GetVelocity(articulation.solver.links[i])+MotionVector.Linear( deltaV));
        }
        public unsafe void ClearVelocity()
        {
            var articulation = GetArticulation();
            for (int i = 0; i < articulation.solver.nLinks; i++)
                ManagedWorld.main.SetVelocity(articulation.solver.links[i], MotionVector.zero);
        }
    }
}
