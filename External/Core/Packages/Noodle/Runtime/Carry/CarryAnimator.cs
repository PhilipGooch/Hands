using Noodles.Animation;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    /// <summary>
    /// allows to overlay up to 2 animations on base animation
    /// </summary>
    public struct AnimationBlend
    {
        public CarryableAnimationRef a0;
        public CarryableAnimationRef a1;
        bool engage;
        float transitionDuration;
        public float w0;
        public float w1;

        //float blendW2 => 1;
        //public float blendW1 => w0 < 1 ? w1 / (1 - w0) : 0;
        //public float blendW0 => w0;


        // blend
        // p = I*(1-c-r)+R*r+C*c;
        // do 2 linear blends (I vs R) vs C
        // p = (I*(1-c-r)/(1-c)+R*r/(1-c))*(1-c)+C*c;

        public void Step(float dt)
        {
            if (transitionDuration == 0) { w0 = engage ? 1 : 0; w1 = 0; }
            else
            {
                if (engage)
                {
                    w0 = re.MoveTowards(w0, 1, dt / transitionDuration);
                    w1 = math.min(w1, 1 - w0);
                }
                else if (w0 > 0 || w1 > 0)
                {
                    var wIdle = 1 - w0 - w1;
                    var newWIdle = re.MoveTowards(wIdle, 1, dt / transitionDuration);
                    w0 *= (1 - newWIdle) / (1 - wIdle);
                    w1 *= (1 - newWIdle) / (1 - wIdle);
                }
            }
        }

        public void TransitionTo(CarryableAnimationRef anim, float transitionTime)
        {
            transitionDuration = transitionTime;
            if (anim.isEmpty)
            {
                engage = false;
            }
            else
            {
                engage = true;

                if (anim.Equals(a0)) { }
                else if (anim.Equals(a1)) { a1 = a0; a0 = anim; var t = w1; w1 = w0; w0 = t; }
                else // replace animation that has smaller weight
                {
                    if (w1 <= w0) { a1 = a0; w1 = w0; }
                    a0 = anim;
                    w0 = 0;
                }
            }
        }

        public override string ToString() => $"{w0} {a0} <- {w1} {a1}";

    }

    public unsafe static class CarryAnimator
    {
        public const float CARRY_TRANSITION_TIME = 0.2f; // transition inside carry state (e.g. reach vs two handed)
        public const float CARRY_ENGAGE_TIME = .01f; // instant
        public const float CARRY_DISENGAGE_TIME = .01f;//instant
        public const float SWING_ENGAGE_TIME = .5f;


        private static void OnReach(in NoodleAnimatorData animator, ref HandCarryData hand, ref HandCarryData othr, ref CarryData data)
        {

            var carryDB = animator.animationDB.carry;
            // if other.hold && exists reach anim =>  transition all to reach anim
            if (othr.state == HandState.Hold && othr.type != 0 && othr.allowReach && carryDB.Contains(othr.type, CarryableAnimationType.Reach))
            {
                //Debug.Log("OnReach: Carryable");
                var anim = new CarryableAnimationRef(othr.type, CarryableAnimationType.Reach, othr.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                hand.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
            }
            // else if other idle => transtition torso to reach, transition me to reach
            else if (othr.state == HandState.Idle)
            {
                //Debug.Log("OnReach: first hand");
                var anim = CarryableAnimationRef.defaultReach;
                data.anim.TransitionTo(anim, CARRY_ENGAGE_TIME);
                hand.anim.TransitionTo(anim, CARRY_ENGAGE_TIME);
            }
            // else => transition me to reach
            else
            {
                //Debug.Log("OnReach: second hand");
                var anim = CarryableAnimationRef.defaultReach;
                hand.anim.TransitionTo(anim, CARRY_ENGAGE_TIME);
            }
        }
        private static void OnIdle(in NoodleAnimatorData animator, ref HandCarryData hand, ref HandCarryData othr, ref CarryData data)
        {

            var carryDB = animator.animationDB.carry;
            // if other.hold && exists carry anim => transition all to carry anim
            if (othr.state == HandState.Hold && othr.type != 0 && carryDB.Contains(othr.type, CarryableAnimationType.Carry))
            {
                //Debug.Log("OnIdle: Carryable");
                var anim = new CarryableAnimationRef(othr.type, CarryableAnimationType.Carry, othr.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                hand.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
            }
            // else if other idle => transtion torso idle, transition me idleelse if(othr.state == HandState.Idle)
            else if (othr.state == HandState.Idle)
            {
                //Debug.Log("OnIdle: last hand");
                var anim = CarryableAnimationRef.empty;
                data.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
                hand.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
            }
            // else => transition to idle
            else
            {
                //Debug.Log("OnIdle: single hand");
                var anim = CarryableAnimationRef.empty;
                hand.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
            }
        }


        private static void OnGrabbed(in NoodleAnimatorData animator, ref HandCarryData hand, ref HandCarryData othr, ref CarryData data)
        {

            var carryDB = animator.animationDB.carry;
            // if other.hold and same and exist twohanded => transition all to twohanded
            if (othr.state == HandState.Hold && hand.blockId == othr.blockId && hand.type != 0 && hand.allowTwoHanded && carryDB.Contains(hand.type, CarryableAnimationType.TwoHanded))
            {
                //Debug.Log("OnGrabbed: double carryable");
                var anim = new CarryableAnimationRef(hand.type, CarryableAnimationType.TwoHanded, data.leftMain);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                hand.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
            }
            // else if other hold => transition torso and other to carry, me to independent carry
            else if (othr.state == HandState.Hold)
            {
                //Debug.Log("OnGrabbed: two independent");
                var anim = new CarryableAnimationRef(othr.type, CarryableAnimationType.Carry, othr.isLeft);
                if (!othr.allowCarry || !carryDB.Contains(anim)) anim = CarryableAnimationRef.defaultCarry(othr.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                anim = new CarryableAnimationRef(hand.type, CarryableAnimationType.Carry, hand.isLeft);
                if (!hand.allowCarry || !carryDB.Contains(anim)) anim = CarryableAnimationRef.defaultCarry(hand.isLeft);
                hand.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
            }
            // else if other reach and exists reach => transition all to reach
            else if (othr.state == HandState.Grab && hand.type != 0 && hand.allowReach && carryDB.Contains(hand.type, CarryableAnimationType.Reach))
            {
                //Debug.Log("OnGrabbed: carryable, other is reaching");
                var anim = new CarryableAnimationRef(hand.type, CarryableAnimationType.Reach, hand.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                hand.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
            }
            // else if exists carry=> transition all to carry
            else if (hand.type != 0 && hand.allowCarry && carryDB.Contains(hand.type, CarryableAnimationType.Carry))
            {
                //Debug.Log("OnGrabbed: single carryable");
                var anim = new CarryableAnimationRef(hand.type, CarryableAnimationType.Carry, hand.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                hand.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
            }
            // else => transtion torso to hold, transition me to hold
            else
            {
                //Debug.Log("OnGrabbed: single default hold");
                var anim = CarryableAnimationRef.defaultCarry(hand.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                hand.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
            }

        }
        private static void OnReleased(in NoodleAnimatorData animator, ref HandCarryData hand, ref HandCarryData othr, ref CarryData data)
        {

            var carryDB = animator.animationDB.carry;
            // if other.hold and exists other.carry=> transition all to other.carryif (carryDB.Contains(hand.type, CarryableAnimationType.Carry))
            if (othr.state == HandState.Hold && othr.type != 0 && othr.allowCarry && carryDB.Contains(othr.type, CarryableAnimationType.Carry))
            {
                //Debug.Log("OnReleased: other still carryable");
                var anim = new CarryableAnimationRef(othr.type, CarryableAnimationType.Carry, othr.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                hand.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
            }
            // else if other.hold transition to default hold, other to default hold, me to idle
            else if (othr.state == HandState.Hold)
            {
                //Debug.Log("OnReleased: other default hold");
                var anim = CarryableAnimationRef.defaultCarry(othr.isLeft);
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                anim = CarryableAnimationRef.empty;
                hand.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
            }
            // else if other reach => transition torso, other to default reach, me to idle
            else if (othr.state == HandState.Grab)
            {
                //Debug.Log("OnReleased: other reaches");
                var anim = CarryableAnimationRef.defaultReach;
                data.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                othr.anim.TransitionTo(anim, CARRY_TRANSITION_TIME);
                anim = CarryableAnimationRef.empty;
                hand.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
            }
            // if other idle => all to idle
            else
            {
                //Debug.Log("OnReleased: full idle");
                var anim = CarryableAnimationRef.empty;
                data.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
                hand.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
                othr.anim.TransitionTo(anim, CARRY_DISENGAGE_TIME);
            }
        }

        private static void DetectTransitions(in NoodleAnimatorData animator, ref HandCarryData hand, ref HandCarryData othr, ref CarryData data)
        {

            if (hand.lastState != HandState.Hold && hand.state == HandState.Hold)
                OnGrabbed(animator, ref hand, ref othr, ref data);
            if (hand.lastState == HandState.Hold && hand.state != HandState.Hold)
                OnReleased(animator, ref hand, ref othr, ref data);
            if (hand.lastState == HandState.Idle && hand.state == HandState.Grab)
                OnReach(animator, ref hand, ref othr, ref data);
            if (hand.lastState == HandState.Grab && hand.state == HandState.Idle)
                OnIdle(animator, ref hand, ref othr, ref data);
            hand.lastState = hand.state;
        }

        public static void CalculateAnimations(in NoodleAnimatorData animator, in NoodleState state, ref CarryData carry)
        {

            DetectTransitions(animator, ref carry.l, ref carry.r, ref carry);
            DetectTransitions(animator, ref carry.r, ref carry.l, ref carry);

            var dt = World.main.dt;
            carry.anim.Step(dt);
            carry.l.anim.Step(dt);
            carry.r.anim.Step(dt);
        }
        // calculates pose that could be layered on idle pose to implement carryable animation
        public static void GetPose(NoodleAnimatorData data, in NoodleState state, ref CarryData carry, in Aim aim, in NoodleDimensions dim, out NoodlePose pose,
            out float weightL, out float weightR, out float weightT)
        {
            var carryDB = data.animationDB.carry;
            GetTorsoPose(carry, out pose, out weightT, aim, carryDB);

            GetHandPose(carry.l, ref pose.handL, out weightL,aim, carryDB);
            GetHandPose(carry.r, ref pose.handR, out weightR,aim, carryDB);
        }

        private static void GetTorsoPose(CarryData carry, out NoodlePose pose, out float weight, Aim aim, CarryableAnimator carryDB)
        {
            var anim = carry.anim;
            weight = anim.w0 + anim.w1;

            if (weight > 0)
            {
                if (anim.w1 > 0)
                {
                    pose = carryDB.GetPose01(anim.a1, aim.pitch01);
                    if (anim.w0 > 0)
                        pose = NoodlePose.Blend(pose, carryDB.GetPose01(anim.a0, aim.pitch01), anim.w0 / weight);
                }
                else
                    pose = carryDB.GetPose01(anim.a0, aim.pitch01);
            }
            else
                pose = default;
        }
       
        private static void GetHandPose(in HandCarryData data, ref HandPose hand, out float weight,  Aim aim, CarryableAnimator carryDB)
        {
            var anim = data.anim;
            weight = anim.w0 + anim.w1;

            if (weight > 0)
            {
                if (anim.w1 > 0)
                {
                    hand = carryDB.GetHandPose01(anim.a1, aim.pitch01, data.isLeft);
                    if (anim.w0 > 0)
                        hand = HandPose.Blend(hand, carryDB.GetHandPose01(anim.a0, aim.pitch01, data.isLeft), anim.w0 / weight, true, true);
                }
                else if (anim.w0 > 0)
                    hand = carryDB.GetHandPose01(anim.a0, aim.pitch01, data.isLeft);
            }
        }
        public static void ApplyPivot(in HandCarryData data, in HandCarryData other, in NoodleAnimatorData animator, ref HandPose pose, ref PivotPose pivot, float pitch01, bool left)
        {
            var carryDB = animator.animationDB.carry;
            var anim = data.anim;
            // blend pivot between w0 and w1
            var weight = anim.w0 + anim.w1;
            if (weight > 0)
            {

                if (anim.w1 > 0)
                {
                    pivot = carryDB.GetPivotPose01(anim.a1, pitch01);
                    if (anim.w0 > 0)
                        pivot = PivotPose.Blend(pivot, carryDB.GetPivotPose01(anim.a0, pitch01), anim.w0 / weight, true, true);
                }
                else if (anim.w0 > 0)
                    pivot = carryDB.GetPivotPose01(anim.a0, pitch01);
            }


            //if (anim.w1 == 0)
            //    carryDB.TryGetPivotPose01(anim.a0, pitch01, out pivot);
            //else
            //{
            //    carryDB.TryGetPivotPose01(anim.a1, pitch01, out pivot);
            //    pivot = carryDB.TryBlendPivotPose01(pivot, anim.a0, pitch01, anim.w0, left);// get from DB or fallback to reference pose (incoming pose)
            //}
        }
    }
}
