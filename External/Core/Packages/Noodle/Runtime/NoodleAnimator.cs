using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using NBG.Entities;
using Unity.Mathematics;
using UnityEngine;
using Noodles.Animation;
using NBG.Unsafe;
using NBG.Core;

namespace Noodles
{
    public enum TurnAnimationState
    {
        None,
        TurnRight,
        TurnLeft
    }
    public struct NoodleAnimatorData
    {
        public NoodleAnimationDatabase animationDB;

        private int _usePoseOverride;
        public bool usePoseOverride { get => _usePoseOverride != 0; set => _usePoseOverride = value ? 1 : 0; }

        public NoodlePose poseOverride;

        // state: torso world rotations to allow twist
        //public float hipsTwist;
        public float chestTwist;
        public float lastClimbPitch01;
        public float climbSelector; // 0-left leg up, 1-right leg up
        public float idleAnimationTime;
        public WeightVector8 coreWeights;
        public float generalTime;
        public float walkTime;

        public WeightVector8 stateWeights;
        public WeightVector4 jumpWeights;

        // feet IK state
        public float walkStartTransition;
        public float3 walkStartOffset;
        private int _groundLeft;
        public bool groundLeft { get => _groundLeft != 0; set => _groundLeft = value ? 1 : 0; }

        // turn in place
        public bool liftedLeft;
        public float walkcycleTime;
        public float twistL;
        public float twistR;
        public TurnAnimationState twistState;

        public float3 walkForward;
        public bool walkState;
        public float timeSinceStep;
        public bool lastStepLeft;
        public float runBlend;
    }

    public partial class NoodleAnimator : MonoBehaviour
    {
        Entity entity;

        //public NoodlePoses poses;
        [NonSerialized] public bool usePoseOverride;
        [NonSerialized] public NoodlePose poseOverride;

        public PhysicalAnimation idle, grab, hold, climb, swing, freeFall, hurt;

        [Space]
        public PhysicalAnimation[] idleAnimations;

        [Space]
        public RingAnimationData walkAnimations;
        public RingAnimationData runAnimations;
        public PhysicalAnimation stepInPlace;

        [Space]
        public PhysicalAnimation jump;
        public RingAnimationData fall;

        [Space]
        public CarryableAnimationDB carry;

        [Space]
        public PhysicalAnimation wakeUpProne;
        public PhysicalAnimation wakeUpSupine;
        //public EmoteAsset wakeUp;

        [Space]
        public EmoteAsset emote0, emote1, emote2, emote3;

#if UNITY_EDITOR
        [NonSerialized]
        [HideInInspector]
        public static IOnFixedUpdate animationEditorPlaybackController;
        public static NoodleAnimator previewAnimator;
#endif

        public ref NoodleDimensions dimensions => ref EntityStore.GetComponentData<NoodleDimensions>(entity);
        public unsafe void OnCreate(Entity entity)
        {
            this.entity = entity;
            ref var animatorData = ref EntityStore.GetComponentData<NoodleAnimatorData>(entity);
            ref var dim = ref EntityStore.GetComponentData<NoodleDimensions>(entity);
            animatorData.animationDB = NoodleAnimationDatabase.Get(this, dim);
            //animatorData.coreWeights = new WeightVector(5);
            //animatorData.stateWeights = new WeightVector(CoreStates.Count);

#if UNITY_EDITOR
            previewAnimator = this;
#endif
        }

        public unsafe void RebakeAnimations()
        {
            ref var animatorData = ref EntityStore.GetComponentData<NoodleAnimatorData>(entity);
            ref var dim = ref EntityStore.GetComponentData<NoodleDimensions>(entity);
            animatorData.animationDB.Rebake(this, dim);
        }
        public void RebakeAnimation(NativeAnimation nativeAnimation, PhysicalAnimation animation)
        {
            ref var dim = ref EntityStore.GetComponentData<NoodleDimensions>(entity);
            nativeAnimation.Bake(animation, dim, true);
        }
        public NativeAnimation BakeAnimation(PhysicalAnimation animation, bool loop)
        {
            ref var dim = ref EntityStore.GetComponentData<NoodleDimensions>(entity);
            return NativeAnimation.Create(animation, dim, loop);
        }

        public void Dispose()
        {
            //ref var animatorData = ref EntityStore.GetComponentData<NoodleAnimatorData>(entity);
            //animatorData.coreWeights.Dispose();
            //animatorData.stateWeights.Dispose();
            NoodleAnimationDatabase.ReleaseRef();
        }

        public static bool showDebugGizmos = false;

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos)
                return;

            var rig = EntityStore.GetComponentObject<NoodleRig>(entity, optional: true);
            if (rig == null)
                return;

            ref var animatorData = ref EntityStore.GetComponentData<NoodleAnimatorData>(entity);
            if (!animatorData.usePoseOverride)
                return;

            ref var dim = ref EntityStore.GetComponentData<NoodleDimensions>(entity);
            ref var inputFrame = ref EntityStore.GetComponentData<InputFrame>(entity);
            ref var articulation = ref World.main.GetArticulation(EntityStore.GetComponentData<ArticulationRef>(entity).articulationId);
            var cg = rig.fullCenterOfMass;
            var bodies = rig.GetArticulation().GetBodies();
            //var cgNoBall = rig.GetArticulation().CalculateCenterOfMass(1, 12);
            var poseTransform = new RigidTransform(quaternion.RotateY(inputFrame.lookYaw), rig.rootPosition);
            var t = NoodlePoseTransforms.GetBodyTransforms(poseOverride, dim).Transform(poseTransform);

            var poseCG = t.GetCenterOfMass(dim);
            //for (int i = 0; i <= 10; i++)
            var error = math.length((poseCG - cg).ZeroY());
            Gizmos.color = Color.gray;
            Gizmos.DrawSphere(cg.ZeroY(), .05f);

            Gizmos.color = error < .01f ? Color.green : error < .05f ? Color.yellow : Color.red;
            Gizmos.DrawSphere(poseCG.ZeroY(), .05f);

            Gizmos.DrawSphere(t.hips.pos, .05f);
            Gizmos.DrawSphere(t.waist.pos, .05f);
            Gizmos.DrawSphere(t.chest.pos, .05f);
            Gizmos.DrawSphere(t.head.pos, .05f);
            Gizmos.DrawSphere(t.upperArmL.pos, .05f);
            Gizmos.DrawSphere(t.upperArmR.pos, .05f);
            Gizmos.DrawSphere(t.lowerArmL.pos, .05f);
            Gizmos.DrawSphere(t.lowerArmR.pos, .05f);
            Gizmos.DrawSphere(t.upperLegL.pos, .05f);
            Gizmos.DrawSphere(t.upperLegR.pos, .05f);
            Gizmos.DrawSphere(t.lowerLegL.pos, .05f);
            Gizmos.DrawSphere(t.lowerLegR.pos, .05f);

            Gizmos.color = Color.gray;
            for (var i = 1; i < 13; i++)
                Gizmos.DrawSphere(bodies.GetBody(i).x.pos, .05f);
        }

        public unsafe void OnFixedUpdate()
        {
#if UNITY_EDITOR
            if (animationEditorPlaybackController != null)
            {
                animationEditorPlaybackController.OnFixedUpdate();
            }
#endif

            ref var animatorData = ref EntityStore.GetComponentData<NoodleAnimatorData>(entity);

            //animatorData.poses = poses.ToRadians();
            animatorData.usePoseOverride = usePoseOverride;
            animatorData.poseOverride = poseOverride;
            animatorData.generalTime += Time.fixedDeltaTime;
        }

        private static class CoreStates
        {
            public const int Idle = 0;
            public const int Reach = 1;
            public const int Hold = 2;
            public const int ClimbSwing = 3;
            public const int Slide = 4;
            //public const int Fall = 5;

            public const int FreeFall = 5;
            public const int Hurt = 6;
            public const int Dead = 7;

            public const int Count = 8;
        }
        public static NoodlePose GetPose(ref NoodleAnimatorData data, ref NoodleState state, in NoodleData noodle, ref CarryData carry, in Aim aim, float dt, in NoodleDimensions dim)
        {
            var SPEED = 2.5f; //TODO: use actual
            // get animation for carryables
            CarryAnimator.CalculateAnimations(data, state, ref carry);
            CarryAnimator.GetPose(data, state, ref carry, aim, dim, out var carryPose, out var carryL, out var carryR, out var carryT);

            // Emotes. Calculate animation, UB emotes independently blend torso and hands, FB emotes are implemented as full layer
            var emotePose = NoodlePose.defaultPose;
            var emoteWeight = state.emote.GetWeight(state.emoteState, state.emoteTimer, state.emoteCancelTimer);
            if (emoteWeight > 0)
                emotePose = state.emote.animation.GetPose(state.emoteTimer);

            // get animation for locomotion
            BaseLocomotionLayer(out var locoPose, ref data, state, noodle, aim, dt, dim); // Breathe, Walk, Turn
                                                                                          // Jump lean

            var transitionDuration = .2f;
            // determine which core to use
            int s = CoreStates.Idle;
            switch (state.state)
            {
                case MainState.Normal:
                case MainState.Jump:
                case MainState.Fall:
                case MainState.WakeUp:
                case MainState.Slide:
                    if (state.handStateL == HandState.Grab || state.handStateR == HandState.Grab) s = CoreStates.Reach;
                    else if (state.handStateL == HandState.Hold || state.handStateR == HandState.Hold) s = CoreStates.Hold;
                    else s = CoreStates.Idle;
                    break;
                case MainState.Climb: s = CoreStates.ClimbSwing; break;
                //case MainState.Slide: s = CoreStates.Slide; break;
                //case MainState.Fall: s = CoreStates.Fall; break;
                case MainState.FreeFall: s = CoreStates.FreeFall; break;
                case MainState.Hurt: s = CoreStates.Hurt; break;
                case MainState.Dead: s = CoreStates.Dead; break;
            }
            data.stateWeights.Pull(s, dt / transitionDuration);
            var w = data.stateWeights;

            // Multiple layers:
            // * Core (Idle vs Reach vs Hold vs Fall vs UbEmote)
            // * Blend with walkcycle
            // * UbEmote hands
            // * ClimbSwing
            // * Slide overlay
            // * Hurt overlay
            // * FreeFall overlay
            // * Carry arms
            // * Dead overlay
            // * WakeUp overlay
            // * FB Emotes overlay

            var pose = NoodlePose.defaultPose;

            var total = w[CoreStates.Idle] + w[CoreStates.Reach] + w[CoreStates.Hold];// + w[CoreStates.Fall];
            if (total > 0)
            {
                var corePose = new NoodlePose();
                if (w[CoreStates.Idle] > 0)
                    corePose = NoodlePose.Add(corePose, data.animationDB.idle.GetPose01(aim.pitch01), w[CoreStates.Idle] / total);
                if (w[CoreStates.Reach] > 0)
                    corePose = NoodlePose.Add(corePose, data.animationDB.grab.GetPose01(aim.pitch01), w[CoreStates.Reach] / total);
                if (w[CoreStates.Hold] > 0)
                    corePose = NoodlePose.Add(corePose, data.animationDB.hold.GetPose01(aim.pitch01), w[CoreStates.Hold] / total);
                //if (w[CoreStates.Fall] > 0)
                //    corePose = NoodlePose.Add(corePose, data.animationDB.idle.GetPose01(aim.pitch01), w[CoreStates.Fall] / total);
                corePose = NoodlePose.Blend(corePose, carryPose, carryT, carryT, 0, 0, 0, 0, true, true);

                var muscleWeight = 1 - w[CoreStates.Idle] / total; // non idle poses change locomotion muscles, idle passes through
                // upper body emote (blend idle+core vs emote)
                if (state.emoteState != EmoteState.None && state.emote.type == EmoteType.UpperBody)
                {
                    corePose = NoodlePose.Blend(corePose, emotePose, emoteWeight);
                    muscleWeight = 1 - (1 - muscleWeight) * (1 - emoteWeight); // passtrhough part get smaller when moteWeight grows
                }
                var additiveCorePose = NoodlePose.Add(corePose, data.animationDB.idle.GetPose01(.5f), -1); // convert to additive
                pose = NoodlePose.Add(locoPose, additiveCorePose, 1, 1, 0, 0, 0, 0, pose: true, muscle: false); // add head and torso
                pose = NoodlePose.Blend(pose, corePose, muscleWeight, muscleWeight, 0, 0, 0, 0, pose: false, muscle: true); // override torso muscle

                ApplyPoseTwist(ref pose, ref data); // Spine twist
            }

            // upper body emote - override hands
            if (state.emoteState != EmoteState.None && state.emote.type == EmoteType.UpperBody)
            {
                pose.handL = HandPose.Blend(pose.handL, emotePose.handL, emoteWeight, true, true);
                pose.handR = HandPose.Blend(pose.handR, emotePose.handR, emoteWeight, true, true);
            }

            if (state.state != MainState.Climb)
                data.lastClimbPitch01 = -1; // reset

            total += w[CoreStates.ClimbSwing];
            if (w[CoreStates.ClimbSwing] > 0)
            {
                GetPoseClimb(ref data, ref state, aim, dt, out var climbPose, dim);
                var t = w[CoreStates.ClimbSwing] / total;
                pose = NoodlePose.Blend(pose, climbPose, t, t, carry.l.state == HandState.Hold ? t : 0, carry.r.state == HandState.Hold ? t : 0, t, t, true, true);
                if (carry.l.state == HandState.Hold)
                    carryPose.handL = HandPose.Blend(carryPose.handL, climbPose.handL, w[CoreStates.ClimbSwing] / total, true, true);
                if (carry.r.state == HandState.Hold)
                    carryPose.handR = HandPose.Blend(carryPose.handR, climbPose.handR, w[CoreStates.ClimbSwing] / total, true, true);
            }

            // Overlays before arms
            total += w[CoreStates.Slide];
            if (w[CoreStates.Slide] > 0)
                pose = NoodlePose.Blend(pose, data.animationDB.idle.GetPose01(aim.pitch01), w[CoreStates.Slide] / total);

            total += w[CoreStates.Hurt];
            if (w[CoreStates.Hurt] > 0)
                pose = NoodlePose.Blend(pose, data.animationDB.hurt.GetPose01(aim.pitch01), w[CoreStates.Hurt] / total);

            total += w[CoreStates.FreeFall];
            if (w[CoreStates.FreeFall] > 0)
                pose = NoodlePose.Blend(pose, data.animationDB.freeFall.GetPose01(aim.pitch01), w[CoreStates.FreeFall] / total);

            JumpFallLayer(ref data, state, noodle, aim, dim, SPEED, ref pose);

            // Hands
            if (total > 0)
            {
                pose.handL = HandPose.Blend(pose.handL, carryPose.handL, carryL, true, true);
                pose.handR = HandPose.Blend(pose.handR, carryPose.handR, carryR, true, true);
            }

            // full body emote
            if (state.emoteState != EmoteState.None && state.emote.type == EmoteType.FullBody)
            {
                if (state.state == MainState.WakeUp) // wake up emotes need to slowly end up at target posture
                {
                    var feetBlend = state.emote.GetWeight(state.emoteState, state.emoteTimer, state.emoteCancelTimer, 1);
                    var torsoBlend = state.emote.GetWeight(state.emoteState, state.emoteTimer, state.emoteCancelTimer, 1.5f);
                    var armsBlend = state.emote.GetWeight(state.emoteState, state.emoteTimer, state.emoteCancelTimer, .5f);
                    pose = NoodlePose.Blend(pose, emotePose, torsoBlend, torsoBlend, armsBlend, armsBlend, feetBlend, feetBlend, true, true);
                }
                else
                    pose = NoodlePose.Blend(pose, emotePose, emoteWeight);
            }

            // Full body overlays (dead, wakeup)
            total += w[CoreStates.Dead];
            if (w[CoreStates.Dead] > 0) // override all muscle with 0
            {
                // hurt but with zero muscle
                pose = NoodlePose.Blend(pose, data.animationDB.hurt.GetPose01(aim.pitch01), w[CoreStates.Dead] / total);
                pose = NoodlePose.Blend(pose, default, w[CoreStates.Dead], 1, 1, 1, 1, 1, false, true);
            }

            return pose;
        }
        private static class JumpStates
        {
            public const int None = 0;
            public const int Jump = 1;
            public const int Fall = 2;
        }
        private static void JumpFallLayer(ref NoodleAnimatorData data, NoodleState state, NoodleData noodle, Aim aim, NoodleDimensions dim, float SPEED, ref NoodlePose pose)
        {
            var transitionDuration = .2f;

            var s = JumpStates.None;
            if (state.state == MainState.Jump || state.state == MainState.Fall)
                if ((state.handStateL == HandState.Grab || state.handStateR == HandState.Grab) && state.handStateL != HandState.Hold && state.handStateR != HandState.Hold)
                    s = JumpStates.Jump;
                else
                    s = JumpStates.Fall;

            data.jumpWeights.Pull(s, transitionDuration);
            var w = data.jumpWeights;
            var total = w[JumpStates.None];
            float jumpTime = state.jumpTimer2 + state.fallTimer2;
            if (w[JumpStates.Jump] > 0)
            {
                total += w[JumpStates.Jump];
                var t = w[JumpStates.Jump] / total;
                var jumpPose = data.animationDB.jump.GetPose(jumpTime);
                pose = NoodlePose.Blend(pose, jumpPose, t, 0, t, t, t, t, true, true);// except head
            }
            if (w[JumpStates.Fall] > 0)
            {
                total += w[JumpStates.Fall];
                var t = w[JumpStates.Fall] / total;
                var fallPose = data.animationDB.fall.Blend(0, 1, jumpTime, dim);
                pose = NoodlePose.Blend(pose, fallPose, t, 0, t, t, t, t, true, true);// except head
            }
        }
    }
}

