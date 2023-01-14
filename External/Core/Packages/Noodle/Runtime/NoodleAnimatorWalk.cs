using Noodles.Animation;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public partial class NoodleAnimator
    {

        private static float CalculateRootVelocity(float ikDist, int ikFrames, int totalFrames)
            => ikDist / ikFrames * totalFrames;

        private static void BaseLocomotionLayer(out NoodlePose pose, ref NoodleAnimatorData data, NoodleState state, NoodleData noodle, Aim aim, float dt, in NoodleDimensions dim)
        {
            ProcessTorsoTwist(ref data, state, noodle.groundAngularVelocity, aim.yawVelocity, dt);
            var WALK_SPEED = 2.5f;
            var WALK_RUN_TRANSITION = .2f;
            var actualMoveVec = (noodle.velocity - noodle.groundVelocity).RotateY(-aim.yaw);

            var targetWalkSpeed = state.moveMagnitude * WALK_SPEED;
            var actualWalkSpeed = math.length(actualMoveVec.ZeroY());

            var animatedWalkSpeed = math.min(targetWalkSpeed, actualWalkSpeed); //limit animation speed if ran against the wall
            if (state.state == MainState.Slide) animatedWalkSpeed = actualWalkSpeed;


            var runTarget = animatedWalkSpeed > 1.5f ? 1 : 0;// re.InverseLerp(1.0f, 1.5f, animatedWalkSpeed);
            if (animatedWalkSpeed > 0.001f)
            {
                data.walkForward = re.forward.RotateY(state.moveYaw);

                if (data.walkState == false) // start walkcycle
                {
                    data.runBlend = runTarget; // starting to walk - can init to whatever
                    if (data.twistState != TurnAnimationState.None) // just continue with the same leg
                        data.twistState = TurnAnimationState.None;
                    else if (data.timeSinceStep < .5f) // do opposite leg
                    {
                        data.liftedLeft = !data.lastStepLeft;
                        data.walkcycleTime = 0;
                    }
                    else
                    {
                        data.liftedLeft = math.dot(state.footPosL, data.walkForward) < math.dot(state.footPosR, data.walkForward); // keep forward foot on ground
                        data.walkcycleTime = 0;
                    }

                    data.walkState = true;
                }
            }
            else if (data.walkState == true) // stop walkcycle
            {
                data.walkState = false;
            }

            if (data.twistState == TurnAnimationState.None && data.walkState == false)
                data.timeSinceStep += dt;
            else if (data.walkcycleTime > .25f) // ending phase of the step that counts
            {
                data.timeSinceStep = 0;
                data.lastStepLeft = data.liftedLeft;
            }

            //data.runBlend = re.MoveTowards(data.runBlend, runTarget, dt / WALK_RUN_TRANSITION);
            var animationScaling = 1f;
            var walkStrideScale = math.lerp(1f, 1.25f, re.InverseLerp(.5f, 1.2f, animatedWalkSpeed)); // increase stride as walking faster
            var runStrideScale = math.lerp(.9f, 1f, re.InverseLerp(1.4f, 2f, animatedWalkSpeed)); // increase stride as walking faster
            if (data.walkState == true)
            {
                var rootMotionPerCycle = math.lerp(
                   CalculateRootVelocity(.5f, 20, 40) * walkStrideScale,
                   CalculateRootVelocity(1f, 24, 40) * runStrideScale,
                   data.runBlend);

                var animationVelocity = math.max(.2f, animatedWalkSpeed);
                animationScaling = animatedWalkSpeed / animationVelocity;

                data.walkcycleTime += animationVelocity * dt / rootMotionPerCycle;

                if (data.walkcycleTime > .5f)// swap legs, continue animation
                {
                    data.walkcycleTime -= .5f;
                    data.liftedLeft = !data.liftedLeft;
                    data.runBlend = runTarget;
                }
            }

            var animationPhase = (data.liftedLeft ? 0 : .5f) + data.walkcycleTime;
            
            var walkPose = data.animationDB.walk.Blend(data.walkForward.x, data.walkForward.z, (animationPhase + 0*1 / 40f) * data.animationDB.walk.sections[0].duration, dim);
            ScalePoseStride(ref walkPose, walkStrideScale * animationScaling);
            var runPose = data.animationDB.run.Blend(data.walkForward.x, data.walkForward.z, (animationPhase + 0 * 8 / 40f) * data.animationDB.run.sections[0].duration, dim);
            ScalePoseStride(ref runPose, runStrideScale * animationScaling);
            pose = data.walkState == true ?
                NoodlePose.Blend(walkPose, runPose, data.runBlend) :
                data.animationDB.idle.GetPose01(.5f);

            if (data.walkState == false && data.twistState != TurnAnimationState.None)
                pose = data.animationDB.stepInPlace.GetPose(animationPhase * data.animationDB.stepInPlace.duration, loop: true);


            //Debug.Log($"{data.walkState} {data.twistState} {data.twistTime} {data.liftedLeft}");

        }
        static void ScalePoseStride(ref NoodlePose p, float s)
        {
            var IDLE_L = new float3(-.2f, 0, 0);
            var IDLE_R = new float3(+.2f, 0, 0);

            float3 scale(float3 v, float3 o, float s) => new float3((v.x - o.x) * s + o.x, v.y, (v.z - o.z) * s + o.z);
            p.legL.ikPos = scale(p.legL.ikPos, IDLE_L, s);
            p.legR.ikPos = scale(p.legR.ikPos, IDLE_R, s);
            p.legL.ikPosRelative = scale(p.legL.ikPosRelative, float3.zero, s);
            p.legR.ikPosRelative = scale(p.legR.ikPosRelative, float3.zero, s);
        }

       

        public static void ProcessTorsoTwist(ref NoodleAnimatorData data, in NoodleState state, float groundAngularVelocity, float aimYawSpeed, float dt)
        {
            var TURN_PREDICT_TIME = .2f;
            var TURN_SPEED = math.radians(120);
            var twistLimit = math.radians(60 - 60 * state.moveMagnitude);

            // twist legs when aim rotates or ground below rotates
            data.twistL += (groundAngularVelocity - aimYawSpeed) * dt;
            data.twistR += (groundAngularVelocity - aimYawSpeed) * dt;


            if (state.moveMagnitude==0 && data.twistState == TurnAnimationState.None)
            {
                var priorityLeft = math.abs(data.twistL) > math.abs(data.twistR);
                var err = priorityLeft ? data.twistL : data.twistR;
                if (err>twistLimit)
                    data.twistState = TurnAnimationState.TurnRight;
                if(err<-twistLimit)
                    data.twistState = TurnAnimationState.TurnLeft;
                if(data.twistState != TurnAnimationState.None)
                {
                    data.liftedLeft = priorityLeft;
                    data.walkcycleTime = 0;
                }
            }


            if (data.twistState != TurnAnimationState.None)
            {
                var timeScale = math.clamp(math.abs(aimYawSpeed), 1, 1.5f);
                data.walkcycleTime += dt * timeScale;

                var speedL = data.liftedLeft ? 3 : 0;
                var speedR = !data.liftedLeft ? 3 : 0;

                data.twistL = re.MoveTowards(data.twistL, aimYawSpeed * TURN_PREDICT_TIME, TURN_SPEED * dt * timeScale * speedL);
                data.twistR = re.MoveTowards(data.twistR, aimYawSpeed * TURN_PREDICT_TIME, TURN_SPEED * dt * timeScale * speedR);

                if (data.walkcycleTime > .5f)
                    data.twistState = TurnAnimationState.None;
            }

            // twist is average of each leg twist
            var hipsTwist = (data.twistL + data.twistR) / 2;

            // untwist when running
            var untwistSpeed = state.moveMagnitude > 0 ? math.max(.25f, state.moveMagnitude) / .2f : 0;
            hipsTwist = re.MoveTowards(hipsTwist, 0, dt * untwistSpeed); // untwist hips
            data.twistL = re.MoveTowards(data.twistL, hipsTwist, dt * untwistSpeed); // realign to hips
            data.twistR = re.MoveTowards(data.twistR, hipsTwist, dt * untwistSpeed); // realign to hips

            // chest constraints to hips and to center
            var grab = state.handStateL != HandState.Idle || state.handStateR != HandState.Idle;
            data.chestTwist += (hipsTwist - data.chestTwist) * dt; // spring to align with hips
            data.chestTwist +=  - data.chestTwist * math.abs( data.chestTwist) * dt * 10; // square- spring with aim
            data.chestTwist = re.MoveTowards(data.chestTwist, 0, dt * (state.moveMagnitude + (grab ? 2 : 0))); // align to aim when moving or grabbing)
        }

        private static void ApplyPoseTwist(ref NoodlePose pose, ref NoodleAnimatorData data)
        {
            var hipsTwist = (data.twistL + data.twistR) / 2;
            var hipsRot = pose.torso.hipsRotation;
            var waistRot = pose.torso.waistRotation;
            var chestRot = pose.torso.chestRotation;

            hipsRot = math.mul(quaternion.RotateY(hipsTwist), hipsRot);
            waistRot = math.mul(quaternion.RotateY((hipsTwist + data.chestTwist) / 2), waistRot);
            chestRot = math.mul(quaternion.RotateY(data.chestTwist), chestRot);

            pose.torso.hipsRotation = hipsRot;
            pose.torso.waistRotation = waistRot;
            pose.torso.chestRotation = chestRot;

            pose.legL.ikPos = pose.legL.ikPos.RotateY(data.twistL);
            pose.legR.ikPos = pose.legR.ikPos.RotateY(data.twistR);
        }
    }
   

}
