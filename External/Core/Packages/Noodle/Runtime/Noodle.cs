using Recoil;
using System;
using NBG.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using NBG.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using NBG.Core.GameSystems;

namespace Noodles
{
    public sealed class GlobalJobData // Used for GameSystem job dependencies
    {
    }

    public struct GravityOverride
    {
        public bool overrideGravity;
        public float3 gravity;
    }

    public struct NoodleData
    {
        //situation
        public float3 rootPos;
        public bool grounded;
        public float slope;
        public bool hanging;
        public float3 acceleration;
        public float3 velocity;
        public float3 groundVelocity;
        public float groundAngularVelocity;

        public bool debug;
        internal ForceVector shoulderForceL;
        internal ForceVector shoulderForceR;
    }
    public class Noodle : MonoBehaviour
    {
        NoodleHand handL;
        NoodleHand handR;
        [NonSerialized] public NoodleRig rig;
        [NonSerialized] public NoodleAnimator animator;


        [NonSerialized]
        public Entity entity;

        internal ref T GetComponentData<T>() where T : unmanaged => ref EntityStore.GetComponentData<T>(entity);
        public bool isGrounded => GetComponentData<BallData>().grounded;


        static ComponentTypeList types;

        public unsafe void OnCreate(Entity entity)
        {
            this.entity = entity;
            if (types.Length == 0)
            {
                types = ComponentTypeList.Create();
                types.AddType<NoodleData>();
                types.AddType<NoodleState>();
                types.AddType<CarryData>();

                types.AddType<GravityOverride>();
                types.AddType<InputFrame>();
                types.AddType<NoodleAnimatorData>();
                types.AddType<RuntimeIKTargets>();

                types.AddType<NoodlePose>();

                types.AddType<Aim>();
            }
            EntityStore.AddComponents(entity, types);
            EntityStore.GetComponentData<CarryData>(entity) = CarryData.empty;
            EntityStore.GetComponentData<NoodleState>(entity).random.InitState();
            var hands = GetComponentsInChildren<NoodleHand>();
            if (hands.Length != 2) throw new InvalidOperationException("Noodle mus have exactly 2 NoodleHands");
            handL = hands[0];
            handR = hands[1];
            rig = GetComponentInChildren<NoodleRig>();
            
            rig.OnCreate(entity, handL.GetHandAnchor(), handR.GetHandAnchor(), new float3(0,NoodleConstants.FOOT_ANCHOR_TEMP,0), new float3(0, NoodleConstants.FOOT_ANCHOR_TEMP, 0));

            handL.OnCreate(entity, rig.dimensions.armL, handR);
            handR.OnCreate(entity, rig.dimensions.armR, handL);
            handL.isLeft = true; handR.isLeft = false;

            handL.handId = rig.GetBodyId(NoodleBones.LowerArmL);
            handR.handId = rig.GetBodyId(NoodleBones.LowerArmR);

            animator = GetComponentInChildren<NoodleAnimator>();
            animator.OnCreate(entity);

            EntityStore.AddComponentObject(entity, rig);

        }
        public void Dispose()
        {
            rig.Dispose();
            animator.Dispose();
            EntityStore.RemoveEntity(entity);
        }

       
        public unsafe void OnFixedUpdate(ref InputFrame inputFrame)
        {
            animator.OnFixedUpdate();
            if (RootMovementOverrides.turnSpeed != 0 || !RootMovementOverrides.moveOverride.Equals(Vector2.zero))
            {
                inputFrame.lookYaw += RootMovementOverrides.turnSpeed * Time.fixedDeltaTime;
                inputFrame.moveYaw = math.atan2(RootMovementOverrides.moveOverride.x, RootMovementOverrides.moveOverride.y);
                inputFrame.moveMagnitude = Mathf.Clamp01(RootMovementOverrides.moveOverride.magnitude);
            }
            var entityRef = EntityStore.GetEntityReference(entity);
            ref var state = ref entityRef.GetComponentData<NoodleState>();
            ref var aim = ref entityRef.GetComponentData<Aim>();
            ref var carry = ref entityRef.GetComponentData<CarryData>();
            ref var noodle = ref entityRef.GetComponentData<NoodleData>();
            ref var ball = ref entityRef.GetComponentData<BallData>();
            ref var animationDB = ref entityRef.GetComponentData<NoodleAnimatorData>().animationDB;
            NoodleStates.ProcessInput(ref state, ref inputFrame, animationDB);
            Aim.ReadInput(inputFrame, ref aim, World.main.dt);
            entityRef.GetComponentData<InputFrame>() = inputFrame;


            
            Profiler.BeginSample("StepGrab");
            StepGrab(inputFrame);
            Profiler.EndSample();

            

            rig.OnFixedUpdate(ref ball, inputFrame.forward, inputFrame.moveMagnitude, state.ignoreGround);
            var newVel = rig.velocity;
            noodle.acceleration = (newVel - noodle.velocity) / World.main.dt;
            noodle.velocity = newVel;
            

            // copy data calculate by rig->ball fixed update
            noodle.groundAngularVelocity = ball.groundAngularVelocity;
            noodle.groundVelocity = ball.groundVelocity;
            noodle.grounded = ball.grounded;
            noodle.slope = ball.slope;


            state.groundForce = ball.groundForce.y;
            noodle.hanging = !noodle.grounded && (
                carry.l.isHoldingStatic || carry.r.isHoldingStatic // holding environment
                || noodle.shoulderForceL.linear.y + noodle.shoulderForceR.linear.y < -100);//// or pulling down strong enough
                //|| carry.l.jointForce.linear.y+carry.r.jointForce.linear.y<-100 ); // or pulling down strong enough




            //var ik = GetComponentInChildren<NoodleIKDebugTargets>(false);
            //if (ik != null && ik.enabled)
            //    ikTargets = new IKTargets()
            //    {
            //        handL = new IKTarget(ik.handL != null ? ik.handL.position : default, ik.weightHandL),
            //        handR = new IKTarget(ik.handR != null ? ik.handR.position : default, ik.weightHandR),
            //        footL = new IKTarget(ik.footL != null ? ik.footL.position : default, ik.weightFootL),
            //        footR = new IKTarget(ik.footR != null ? ik.footR.position : default, ik.weightFootR)
            //    };
            //else
            //    ikTargets = new IKTargets()
            //    {
            //        handL = ikL,
            //        handR = ikR,
            //    };

        }


        public unsafe static void Execute(EntityReference entityRef, in Articulation articulation)
        {
            var profiler = ProfilerMarkers.instance;
            var world = World.main;
            //ref var entity = ref entityRef.GetEntity<NoodleEntity>(); 
            ref var inputFrame = ref entityRef.GetComponentData<InputFrame>();
            ref var ball = ref entityRef.GetComponentData<BallData>();
            ref var noodle = ref entityRef.GetComponentData<NoodleData>();
            ref var state = ref entityRef.GetComponentData<NoodleState>();
            ref var carry = ref entityRef.GetComponentData<CarryData>();
            ref var animator = ref entityRef.GetComponentData<NoodleAnimatorData>();
            ref var suspension = ref entityRef.GetComponentData<NoodleSuspensionData>();
            ref var springs = ref entityRef.GetComponentData<NoodleSprings>();
            ref var ikTargets = ref entityRef.GetComponentData<RuntimeIKTargets>();
            ref var dimensions = ref entityRef.GetComponentData<NoodleDimensions>();
            ref var pose = ref entityRef.GetComponentData<NoodlePose>();
            ref var aim = ref entityRef.GetComponentData<Aim>();
            ref var gravityOverride = ref entityRef.GetComponentData<GravityOverride>();
            var bodies = articulation.GetBodies();

            var n = new NoodleJoints(articulation.solver.joints);
            //var aimPitch01 = inputFrame.lookPitch01;
            var forward = inputFrame.forward;


#pragma warning disable CS0162
            if (false) // debug
            {
                n.torso.angularHips.spring = springs.angular;
                n.torso.waist.spring = springs.waist;
                n.torso.chest.spring = springs.chest;
                n.torso.head.spring = springs.head;
                n.legL.upper.spring = n.legR.upper.spring = springs.upperLeg;
                n.legL.lower.spring = n.legR.lower.spring = springs.lowerLeg;
                n.armL.upper.spring = n.armR.upper.spring = springs.upperArm;
                n.armL.lower.spring = n.armR.lower.spring = springs.lowerArm;
                n.torso.head.rotationMode = RotationTargetMode.SelfOffset;
                n.armL.upper.rotationMode = RotationTargetMode.SelfOffset;
                n.armR.upper.rotationMode = RotationTargetMode.SelfOffset;
                return;
            }
#pragma warning restore CS0162
            // could move to Execute from here
            //TODO: check slide state
            

            // STEP0: assemble state
            noodle.rootPos = bodies.TransformPoint(NoodleBones.Ball, float3.zero) - re.up * ball.radius;
            //state.aimPitch01 = aimPitch01;
            state.moveYaw = inputFrame.moveYaw;
            state.moveMagnitude = inputFrame.moveMagnitude;
            //state.lookYawDelta = inputFrame.lookYaw - state.lastLookYaw;
            //state.lastLookYaw = inputFrame.lookYaw;
            state.handStateL = carry.l.state;
            state.handStateR = carry.r.state;
            //state.handPosRelativeL = (bodies.TransformPoint(NoodleBones.LowerArmL, dimensions.armL.handAnchor) - bodies.TransformPoint(NoodleBones.Chest, dimensions.armL.shoulderAnchor)).RotateY(-inputFrame.lookYaw);
            //state.handPosRelativeR = (bodies.TransformPoint(NoodleBones.LowerArmR, dimensions.armR.handAnchor) - bodies.TransformPoint(NoodleBones.Chest, dimensions.armR.shoulderAnchor)).RotateY(-inputFrame.lookYaw);
            state.handPosL = (bodies.TransformPoint(NoodleBones.LowerArmL, dimensions.armL.handAnchor) - noodle.rootPos).RotateY(-inputFrame.lookYaw);
            state.handPosR = (bodies.TransformPoint(NoodleBones.LowerArmR, dimensions.armR.handAnchor) - noodle.rootPos).RotateY(-inputFrame.lookYaw);
            state.footPosL = (bodies.TransformPoint(NoodleBones.LowerLegL, dimensions.legL.footAnchor) - noodle.rootPos).RotateY(-inputFrame.lookYaw);
            state.footPosR = (bodies.TransformPoint(NoodleBones.LowerLegR, dimensions.legR.footAnchor) - noodle.rootPos).RotateY(-inputFrame.lookYaw);
            state.isProne = bodies.TransformDirection(NoodleBones.Chest, re.forward).y < 0;
            //state.groundForce =  entityRef.GetComponentData<NoodleSuspensionData>().impulse + entityRef.GetComponentData<NoodleSuspensionData>().limitImpulse;
            // STEP1: Input and state machine
            profiler.profileStateMachine.Begin();


            NoodleStates.CalculateState2(ref state, aim, carry, noodle.velocity-noodle.groundVelocity, noodle.acceleration, ref noodle.grounded, noodle.slope, noodle.hanging, world.dt, animator.animationDB);
            NoodleStates.CalculateEmotes(ref state, world.dt, animator.animationDB);
            var jump = NoodleStates.CalculateJump(ref state);
            profiler.profileStateMachine.End();

            // STEP2: process animation & carry to calculate pose
            profiler.profileAnimator.Begin();
            pose = NoodleAnimator.GetPose(ref animator, ref state, noodle, ref carry, aim, world.dt, dimensions);
            Carry.GetPivotPose(animator, carry, aim, ref pose);
            profiler.profileAnimator.End();

            // STEP3: Inverse Kinematics
            if (animator.usePoseOverride)
                pose = animator.poseOverride;
            var poseTransform = new RigidTransform(quaternion.RotateY(inputFrame.lookYaw), noodle.rootPos);
            if ( ((int)NoodleIK.ikMode&(int)NoodleIKMode.Runtime)!=0)
            {
                profiler.profileIK.Begin();
                NoodleIK.SolveInverseKinematics(ref pose, dimensions, recalculateIK: false, poseTransform);
                if(noodle.debug) 
                    NoodlePoseSolver.DebugDraw(pose, dimensions, poseTransform);
                profiler.profileIK.End();
            }

            Carry.ProcessCarryables(ref animator, state, ref carry, articulation, aim, ref pose, dimensions);

   
            pose.grounded = noodle.grounded;


            // STEP4: apply pose
            NoodleRig.ApplyPose(springs, articulation, dimensions, ref suspension, pose, inputFrame.lookYaw, state.state == MainState.Climb, inputFrame.moveMagnitude);

            if (carry.l.state == HandState.Grab)
                SolveStuckHand(bodies, pose.handL, n.armL, poseTransform);
            if (carry.r.state == HandState.Grab)
                SolveStuckHand(bodies, pose.handR, n.armR, poseTransform);

            var gravity = gravityOverride.overrideGravity ? gravityOverride.gravity : World.main.gravity;
            var slope = state.state == MainState.Slide || state.state == MainState.Dead || state.state == MainState.Hurt ? 1f : 0f;// ball.slope; // if not in slide state allow climb

            NoodleRig.ProcessGround(ref ball, inputFrame.forward, inputFrame.moveMagnitude, noodle.velocity, gravity, slope);
            NoodleRig.ApplyVelocities(ref ball, carry, pose, articulation, forward, jump, gravity, state.state == MainState.FreeFall, slope);


            //WriteCarryInfoToCG(articulation, ref carry, n);

            ApplyIKPull(ikTargets.handL, bodies, dimensions.armL, NoodleBones.LowerArmL, carry.r);
            ApplyIKPull(ikTargets.handR, bodies, dimensions.armR, NoodleBones.LowerArmR, carry.l);

            if(state.state == MainState.FreeFall)
                ApplyFreeFall(bodies, ref state);

            Carry.ResetJointForces(ref carry);

        }


        private static void ApplyFreeFall(SolverBodies bodies, ref NoodleState state)
        {
            var m = bodies.CalculateMass(0, bodies.count);
            if (true) // use new
            {

                ref var freeFall = ref state.freeFall;
                if (freeFall.intensityTimer >= freeFall.intensityDuration)
                {
                    freeFall.intensityTimer -= freeFall.intensityDuration;
                    freeFall.intensityDuration = state.random.NextFloat(3) + 2f;
                    freeFall.intensity = state.random.NextFloat(1.5f)+.5f;
                }
                var torqueIntensity = 1f * freeFall.intensity;
                var forceintensity = 1f * freeFall.intensity;

                if (freeFall.forceTimer >= freeFall.forceDuration)
                {
                    freeFall.forceTimer -= freeFall.forceDuration;
                    freeFall.forceDuration = state.random.NextFloat(.75f) + .25f;
                    freeFall.bodyA = freeFall.bodyB;
                    freeFall.forceA = freeFall.forceB;

                    var r = 1;// state.random.NextFloat(1);
                    freeFall.bodyB = state.random.NextInt(bodies.count);
                    freeFall.forceB = RandomOnUnitSphere(ref state.random) * r * 100 * forceintensity;
                    //freeFall.forceB = new float3(freeFall.forceB.x*.1f, math.abs(freeFall.forceB.y),freeFall.forceB.z*.1f);// reduce horizontal range, and only up
                    freeFall.forceB = new float3(freeFall.forceB.x * .1f, freeFall.forceB.y, freeFall.forceB.z * .1f);// reduce horizontal range, and only up

                }
                if (freeFall.torqueTimer >= freeFall.torqueDuration)
                {
                    freeFall.torqueTimer -= freeFall.torqueDuration;
                    freeFall.torqueDuration = state.random.NextFloat(1) + 1;
                    freeFall.torqueA = freeFall.torqueB;
                    var r = state.random.NextFloat(state.random.NextFloat(1f) < .2f ? 1 : .2f); // in one of 5 cases make big impulse
                    freeFall.torqueB = RandomOnUnitSphere(ref state.random) * (r + 0f) * 100 * torqueIntensity;
                }
                var mixA = re.InverseLerp(freeFall.forceDuration, 0, freeFall.forceTimer);
                var mixB = 1 - mixA;

                var impulse = float3.zero;
                bodies.ApplyImpulse(freeFall.bodyA, freeFall.forceA * mixA * World.main.dt);
                bodies.ApplyImpulse(freeFall.bodyB, freeFall.forceB * mixB * World.main.dt);
                impulse += -(freeFall.forceA * mixA + freeFall.forceB * mixB) * World.main.dt;


                bodies.ApplyImpulse(NoodleBones.LowerArmL, re.up * 25 * World.main.dt);
                bodies.ApplyImpulse(NoodleBones.LowerArmR, re.up * 25 * World.main.dt);
                bodies.ApplyImpulse(NoodleBones.LowerLegL, re.up * 25 * World.main.dt);
                bodies.ApplyImpulse(NoodleBones.LowerLegR, re.up * 25 * World.main.dt);
                impulse += re.down * 100 * World.main.dt; // add drag for hands and heet

                for (int i = 0; i < bodies.count; i++)
                    bodies.AddLinearVelocity(i, impulse / m);

                mixA = re.InverseLerp(freeFall.torqueDuration, 0, freeFall.torqueTimer);
                mixB = 1 - mixA;
                var torque = -(freeFall.torqueA * mixA + freeFall.torqueB * mixB) * World.main.dt;
                for (int i = 0; i < bodies.count; i++)
                    bodies.AddAngularVelocity(i, torque);
                freeFall.forceTimer += World.main.dt;
                freeFall.torqueTimer += World.main.dt;
                freeFall.intensityTimer += World.main.dt;

            }
            else
            {

                // HFF1 port
#pragma warning disable CS0162 // Unreachable code detected
                AddRandomTorque(bodies, 0.01f, ref state);
                var mix = math.sin(state.freeFallTimer * 3) * 0.1f;
                state.freeFallTimer += World.main.dt;
                var weight = m;
                bodies.ApplyForce(NoodleBones.Hips, -re.up * weight * mix);
                bodies.ApplyForce(NoodleBones.Chest, re.up * weight * mix);
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        private void LateUpdate()
        {
            return;
#pragma warning disable CS0162 // Unreachable code detected
            var dt = Time.time - Time.fixedTime - Time.fixedDeltaTime;
            var bodies = rig.GetArticulation().GetBodies();
            using(var context = Drawing.DrawingManager.GetBuilder(true))
                context.SphereOutline(bodies.CalculateCenterOfMass(1, 12) + bodies.CalculateVelocity(1, 12) * dt,.3f,Color.black);
#pragma warning restore CS0162 // Unreachable code detected
        }
        public static void AddRandomTorque(SolverBodies bodies, float multiplier, ref NoodleState state)
        {
            var torque = RandomOnUnitSphere(ref state.random) * 100 * multiplier;
            for (int i = 0; i < bodies.count; i++)
                bodies.AddAngularVelocity(i, torque);
        }
        public static float3 RandomOnUnitSphere(ref Unity.Mathematics.Random random)
        {
            float angle1 = random.NextFloat(math.PI*2);
            float angle2 = random.NextFloat(math.PI * 2);

            return new float3 (Mathf.Sin(angle1) * Mathf.Cos(angle2),
            Mathf.Sin(angle1) * Mathf.Sin(angle2),
            Mathf.Cos(angle1));
        }

        private static void ApplyIKPull(in RuntimeIKTarget ik, in SolverBodies bodies, in NoodleArmDimensions dim, int armId, in HandCarryData otherHand)
        {

            if (ik.weight > 0)
            {
                var reachSecondHand = !World.IsEnvironment(ik.bodyId) && otherHand.bodyId == ik.bodyId;
                var ikPullForce = reachSecondHand ?500: 1000; // up to 1000N based on ik weight, much lower when grabbing with second arm
                var ikDamper = 20;// reachSecondHand?0: 20; // damper to match body speed (when grabbing with first arm)

                var actualHandPos = bodies.TransformPoint(armId, dim.handAnchor);// + ikTargets.handR.relativeHandAnchor;
                var vel = bodies.GetWorldPointVelocity(armId, actualHandPos).linear - ik.vel;

                var falloff = re.InverseLerp(0, .1f, math.length(ik.pos - actualHandPos)); // make weaker when close
                var f = math.normalize(ik.pos - actualHandPos) * ikPullForce * falloff - vel * ikDamper;

                f *= ik.weight;
                bodies.ApplyForceAtWorldPos(armId, f, actualHandPos);

                bodies.ApplyForce(NoodleBones.Ball, -f); // feedback to ball
                //NoodleDebug.builder.Line(actualHandPos, ik.worldBodyAnchor, Color.blue);
            }

        }


        public unsafe void PostFixedUpdate()
        {
            rig.DistributeGroundFeedback();
        }

        //public static void WriteCarryInfoToCG(in Articulation articulation, ref CarryData carry, in NoodleJoints n)
        //{
        //    Carry.InferCarriedMass(articulation, ref carry, out n.cg.attachment1pos, out n.cg.attachment1mass);
        //}
        public unsafe static void ExecuteAfterSolve(EntityReference entityRef, in Articulation articulation)
        {
            //Noodle.WriteCarryInfoToCG(articulation, ref entityRef.GetComponentData<CarryData>(), new NoodleJoints(articulation.solver.joints));
            //ref var carry = ref entityRef.GetComponentData<CarryData>();
            //LimitForce(articulation, ref carry.l);
            //LimitForce(articulation, ref carry.r);

        }
        public unsafe static void ExecuteBeforeVelocityIteration(EntityReference entity, int iteration)
        {
            // calculate inferred mass before first velocity iteration (well actually not needed - approximate using past frame data)
            //if (iteration == 0)
            //    ExecuteAfterVelocityIteration(entity, iteration);
        }
        public unsafe static void ExecuteAfterVelocityIteration(EntityReference entity, int iteration)
        {
            var articulation = Recoil.World.main.GetArticulation(entity.GetComponentData<ArticulationRef>().articulationId);
            ref var carry = ref entity.GetComponentData<CarryData>();
            var n = new NoodleJoints(articulation.solver.joints);
            Carry.InferCarriedMass(articulation, ref carry, out n.cg.attachment1pos, out n.cg.attachment1mass);
        }

        //private static unsafe void LimitForce(Articulation articulation, ref HandCarryData hand)
        //{
        //    return;
        //    if (hand.blockId >= 0)
        //    {
        //        var pos = Carry.GetWorldAnchor(hand);
        //        Carry.CalculateJointForce(ref hand);
        //        NoodleDebug.builder.Ray(pos, hand.jointForce.linear / 1000, Color.red);
        //        //if (hand.jointForce.y > 200 ) // trying to lift too strong
        //        //{
        //        //    //var clamped = hand.jointForce.SetY(math.min(hand.jointForce.y,200));
        //        //    var clamped = hand.jointForce.linear.Clamp(200);//.SetY(math.min(hand.jointForce.y, 200));
        //        //    var undoForce = -(hand.jointForce.linear - clamped);

        //        //    if (!World.IsEnvironment(hand.bodyId))
        //        //        World.main.ApplyForceAtWorldPos(hand.bodyId, undoForce, pos);

        //        //    // TODO: distribute force
        //        //    World.main.ApplyForce(articulation.solver.GetBodyId(NoodleBones.Ball), -undoForce);

        //        //}
        //    }
        //}

        // pull hand sideways if stuck behind head or hips
        // uses capsule depenetration (torso is approximated with capsule as well as hand trajectory)
        private static unsafe void SolveStuckHand(SolverBodies bodies, in HandPose pose, in NoodleArmJoints arm, RigidTransform rootTransform)
        {
            var headPos = bodies.TransformPoint(NoodleBones.Head, float3.zero);
            var hipsPos = bodies.TransformPoint(NoodleBones.Hips, float3.zero);
            var handPos = bodies.TransformPoint(arm.IK.link, arm.IK.anchor);
            var targetPos = math.transform(rootTransform, pose.ikPos);

            // calculate handPos->targetPos intersection with bodies and depenetrate
            re.ClosestPtSegmentSegment(headPos, hipsPos, handPos, targetPos, out _, out _, out var torso, out var hand);
            //re.ClosestPtSegmentSegment(headPos, hipsPos, handPos, handPos, out _, out _, out var torso, out var hand);
            //Debug.DrawLine(headPos, hipsPos, Color.yellow);
            //Debug.DrawLine(handPos, targetPos, Color.yellow);
            //Debug.DrawLine(torso, hand, Color.magenta);
            var offset = hand - torso;
            var dist = math.length(offset);
            var TARGET_DIST = .3f;
            var KP = 1000;
            if (dist<TARGET_DIST)// too close
            {
                //Debug.Log(dist);
                var dir = offset / dist;
                var force = dir * (TARGET_DIST-dist) * KP; 
                bodies.ApplyForceAtWorldPos(arm.IK.link, force, handPos); // add force to fist
                bodies.ApplyForce(NoodleBones.Waist, -force); // add opposite force to waist
                
            }

        }


        // Entry point: Game step - activelly scan for targets and grab if found
        public void StepGrab(in InputFrame inputFrame)
        {
            var reach = GetReach(inputFrame);

            // Handle release
            handL.HandleInput(inputFrame.grabL, reach.handL.shoulderPos);
            handR.HandleInput(inputFrame.grabR, reach.handR.shoulderPos);

            // Find targets
            var target = CarryTargetInfo.Empty;
            if (handL.grabState == HandState.Grab)
            {
                var hitCount = handL.CapsuleCast(CarryTargetSearch.neighbours, reach, left: true);
                var otherHand = EntityStore.GetComponentData<CarryData>(entity).GetHand(left: false);
                CarryTargetSearch.ScanTargetInNeighbours(CarryTargetSearch.neighbours, hitCount, reach.handL, reach.aim, ref target.handL, left: true, targetLayers: handL.targetLayers, otherHand);
            }
            if (handR.grabState == HandState.Grab)
            {
                var hitCount = handR.CapsuleCast(CarryTargetSearch.neighbours, reach, left: false);
                var otherHand = EntityStore.GetComponentData<CarryData>(entity).GetHand(left: true);
                CarryTargetSearch.ScanTargetInNeighbours(CarryTargetSearch.neighbours, hitCount, reach.handR, reach.aim, ref target.handR, left: false, targetLayers: handR.targetLayers, otherHand);
            }

            // Grab detected targets
            GrabTargets(target, reach);

            // they were too far - try raycasting
            if (handL.grabState == HandState.Grab)
                handL.Raycast(reach, target.handL);
            if (handR.grabState == HandState.Grab)
                handR.Raycast(reach, target.handR);

            // Update hand springs, etc
            handL.OnFixedUpdate();
            handR.OnFixedUpdate();

            // Set IK
            ref var ikTargets = ref EntityStore.GetComponentData<RuntimeIKTargets>(entity);
            ikTargets.handL = BuildIKTarget(handL, reach.handL, target.handL);
            ikTargets.handR = BuildIKTarget(handR, reach.handR, target.handR);
        }

        private CarryReachInfo GetReach(in InputFrame inputFrame)
        {
            // Calculate target aim positions
            ref var articulation = ref rig.GetArticulation();
            var n = new NoodleJoints(articulation.solver.joints);
            var bodies = articulation.GetBodies();
            GetPoseTarget(bodies, true, n.armL, inputFrame.lookPitch01, inputFrame.lookYaw, out var shoulderL, out var targetL);
            GetPoseTarget(bodies, false, n.armR, inputFrame.lookPitch01, inputFrame.lookYaw, out var shoulderR, out var targetR);

            // Build reach structure
            var reach = new CarryReachInfo()
            {
                aim = EntityStore.GetComponentData<Aim>(entity),
                handL = handL.GetReachInfo(shoulderL, targetL),
                handR = handR.GetReachInfo(shoulderR, targetR),
            };
            return reach;
        }

        public void OnHandCollision(Collider hitCollider, float3 hitPos, bool allowGrabWithoutGrip, bool left)
        {
            //TODO: rebuild reach
            var inputFrame = EntityStore.GetComponentData<InputFrame>(entity); // reuse last input frame
            var reach = GetReach(inputFrame);

            var target = CarryTargetInfo.Empty;
           
            //// Find carryables and grab markers
            var otherHand = EntityStore.GetComponentData<CarryData>(entity).GetHand(left: !left);
            CarryTargetSearch.ScanTargetOnCollision(hitCollider, hitPos, allowGrabWithoutGrip, reach.GetHand(left), reach.aim, ref target.GetHand(left), left, handL.targetLayers, otherHand);

            // grab
            if (left)
                Grab(handL, target.handL, reach, reach.handL);
            else
                Grab(handR, target.handR, reach, reach.handR);
        }

        void Grab(NoodleHand hand, in HandTargetInfo scan, in CarryReachInfo reach, in HandReachInfo handReach)
        {
            if (scan.isEmpty) return; // nothing detected
            var dist = math.length(handReach.actualPos - scan.grabInfo.worldHandPos);
            if (scan.handDist < NoodleHand.targetSnapDistance && dist < NoodleHand.gripSnapDistance)
                hand.Grab(scan.subject, scan.grabInfo, reach.aim);
        }
        public void GrabTargets(in CarryTargetInfo target, in CarryReachInfo reach)
        {
            if (target.handL.error < target.handR.error)
            {
                Grab(handL, target.handL, reach, reach.handL);
                Grab(handR, target.handR, reach, reach.handR);
            }
            else
            {
                Grab(handR, target.handR, reach, reach.handR);
                Grab(handL, target.handL, reach, reach.handL);
            }
        }

        public void GetPoseTarget(SolverBodies bodies, bool left, in NoodleArmJoints arm, float aimPitch01, float yaw, out float3 shoulder, out float3 target)
        {
            shoulder = bodies.TransformPoint(arm.upperLinear.connectedLink, arm.upperLinear.connectedAnchor);

            ref var animator = ref EntityStore.GetComponentData<NoodleAnimatorData>(entity);
            var grabPose = NoodleArmRig.GetGrabPose(animator, aimPitch01);
            target = shoulder + grabPose.GetTargetPos(left).RotateY(yaw);
        }

        public RuntimeIKTarget BuildIKTarget(NoodleHand hand, in HandReachInfo reach, in HandTargetInfo target)
        {
            return hand.grabState == HandState.Grab ?
                new RuntimeIKTarget(target.subject.bodyId, target.grabInfo.worldHandPos, target.grabInfo.worldAnchor, target.ikWeight)
                : new RuntimeIKTarget();
        }

        public void ReleaseHands(float grabDelay = 0)
        {
            handL.ReleaseGrab(grabDelay);
            handR.ReleaseGrab(grabDelay);
        }
        public void GetCarriedBodies(out int grabL, out int grabR)
        {
            ref var carry = ref EntityStore.GetComponentData<CarryData>(entity);
            grabL = carry.l.bodyId;
            grabR = carry.r.bodyId;
        }
        //public void Teleport(float3 offset)
        //{
        //    rig.Teleport(offset);
        //}
        //public void ClearVelocity()
        //{
        //    rig.ClearVelocity();
        //}
    }
    //[BurstCompile]
   
}
