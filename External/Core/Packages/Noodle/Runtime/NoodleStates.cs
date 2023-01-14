using Noodles;
using Noodles.Animation;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Noodles.NoodleStates;

namespace Noodles
{
    public enum MainState
    {
        Normal, // Idle, Reach, Hold, // standing on ground, pose based on hand state
        Climb, // climb <-> swing blend
        Slide,
        Jump, Fall, FreeFall,
        Hurt, // E.g. in tubes
        Dead, WakeUp,
    }




    //public enum MainState { Grounded, Jump, Fall, FreeFall, Climb, Swing, Dead }
    public enum EmoteState { None, Playing, Cancelled }
    public enum HandState { Idle, Grab, Hold }


    public struct NoodleState
    {

        public MainState state;
        public float deadCountdown2;
        public float hurtCountdown2;
        //public bool wakeUpProne;
        //public float wakeUpTimer2;
        public float fallTimer2;
        public float jumpTimer2;
        //public float emoteTimer2;
        //public float slideCountdown2;

        // variables
        //public float aimPitch01;
        public float moveYaw;
        public float moveMagnitude;
        public float moveForward => math.cos(moveYaw) * moveMagnitude;
        public float moveRight => math.sin(moveYaw) * moveMagnitude;

        // emote player
        //public float emoteCountdown;
        //public float emoteInputBlockCountdown;

        //public bool emoteRequiresGroundedPlayer;
        //public int selectedEmote;
        public EmoteState emoteState;
        public Emote emote;
        public bool emoteIsIdle;
        public float emoteTimer;
        public float emoteCancelTimer;
        public float idleTimeWithoutEmotes;
        public float nextIdleAnimationSchedule;
        public float2 lastCameraLook;

        public HandState handStateL;
        public HandState handStateR;
        public float3 handPosL;
        public float3 handPosR;
        public float3 footPosL;
        public float3 footPosR;
        public bool isProne; // facing down

        public float groundForce;

        public float swingBlend;

        public bool ignoreGround =>
            state == MainState.Jump && jumpTimer2 < Timers.JUMP_WINDUP_TIME;
        public FreeFallData freeFall;
        public float freeFallTimer;
        public Unity.Mathematics.Random random;

        public int jumpFrames;
        public int groundFrames;

    }
    public struct FreeFallData
    {
        public float intensity;
        public float intensityTimer;
        public float intensityDuration;

        public int bodyA;
        public float3 forceA;
        public int bodyB;
        public float3 forceB;
        public float forceDuration;
        public float forceTimer;

        public float3 torqueA;
        public float3 torqueB;
        public float torqueDuration;
        public float torqueTimer;
    }
    public class NoodleStates
    {


        public static class Timers
        {
            
            public const int GAME_STEP_HZ = 50;
            public static int MillisecondsToFrames(int ms) => (ms+ GAME_STEP_HZ-1) / GAME_STEP_HZ; // round up
            public const float PLAYHURT_TIME = .5f;
            public const float PLAYDEAD_TIME = 1;
            public const float FALLDEAD_TIME = 1;
            public const float WAKEUP_CANCELABLE_TIME = 1;
            //public const float SLIDE_RECOVERTIME = .1f;

            public const float JUMP_WINDUP_TIME = .2f; // don't go to grounded state to allow leaving ground
            public const float JUMP_COOLDOWN_TIME = .5f;
            public const float JUMP_TO_FALL_TIME = .25f;

            public const float WAKEUP_SPEED_TRESHOLD = 1f;
            public const float WAKEUP_ACCELERATION_TRESHOLD = 1f;
            public static int JUMP_GRACE_FRAMES => MillisecondsToFrames(200); // how many frames jump can trigger afer press (e.g. was in air)
            public static int GROUND_GRACE_FRAMES => MillisecondsToFrames(200); // how many frames jump can trigger afer seeing the ground

            public const float IDLE_ANIMATION_MIN = 2;
            public const float IDLE_ANIMATION_MAX = 15;
        }

       
        public static void CalculateState2(ref NoodleState data, in Aim aim, in CarryData carry, float3 velocity, float3 acceleration, ref bool grounded, float slope, bool holdingStatic, float dt, in NoodleAnimationDatabase animationDB)
        {
            if (grounded && slope==0)
                data.groundFrames = 0;
            else
                data.groundFrames++;
            if(data.groundFrames<Timers.GROUND_GRACE_FRAMES)
            {
                grounded = true;
                slope = 0;
            }
            var supported = grounded || velocity.y > -9.9f && acceleration.y > -.1f; // if grounded or something prevents freefall
            var stationary = math.length(acceleration) < Timers.WAKEUP_ACCELERATION_TRESHOLD || math.length(velocity) < Timers.WAKEUP_SPEED_TRESHOLD; // stopped or moving at constant speed
            if (data.state== MainState.Dead) 
            {
                data.deadCountdown2 -= dt;
                if(data.deadCountdown2<0 && !supported && velocity.y < -9.9f) { data.state = MainState.FreeFall; return; }
                if (data.deadCountdown2 > 0 || !supported || !stationary) return; // stay in dead
                data.state = MainState.WakeUp; // transition to wakeup
                PlayWakeUp(ref data, data.isProne, animationDB);
                
            }
            if(data.state == MainState.Hurt)
            {
                data.hurtCountdown2 -= dt;
                if (data.hurtCountdown2 < 0 && !grounded && velocity.y < -9.9f) { data.state = MainState.FreeFall; return; }
                if (data.hurtCountdown2 > 0 || !grounded || !stationary) return; // stay in ragdoll
                data.state = MainState.WakeUp; // transition to wakeup
                PlayWakeUp(ref data, data.isProne, animationDB);
            }
            if(data.state == MainState.WakeUp)
            {
                if (!grounded) { data.state = MainState.Fall; data.fallTimer2 = 0; return; }
                if (data.emoteTimer < data.emote.animation.duration) return; // stay in wakeup
                data.state = MainState.Normal; // process as normal
            }
            if(data.state == MainState.Jump)
            {
                data.jumpTimer2 += dt;
                if (data.jumpTimer2 < Timers.JUMP_WINDUP_TIME) { grounded = false; return; } // locked in jump for some time
                if (grounded) 
                    data.state = MainState.Normal; // landed
                else
                {
                    if (data.jumpTimer2 >= Timers.JUMP_TO_FALL_TIME) { data.state = MainState.Fall; data.fallTimer2 = 0; }
                    else return;// stay in jump
                }
            }

            var enterClimb = !grounded && holdingStatic;
            if (enterClimb || data.state == MainState.Climb)
            {
                var holdingBothArms = data.handStateL == HandState.Hold && data.handStateR == HandState.Hold;
                var pushingGround = data.groundForce > 100;
                var lookingDown = aim.pitch01 > .5f;

                var stayClimb = holdingBothArms && (!pushingGround || lookingDown);// holding, and no ground support or climbed ledge
                if (enterClimb || stayClimb)  
                {
                    data.state = MainState.Climb;
                    var swingTarget = (data.handStateL == HandState.Hold && data.handStateR == HandState.Hold) ?
                                            re.InverseLerp(.75f, .25f, aim.pitch01) : 1; // if holding with two arms and look down - climb, otherwise swing
                    data.swingBlend = swingTarget;// re.MoveTowards(data.swingBlend, swingTarget, dt);
                    return;
                }

            }
            if (!grounded)
            {
                data.fallTimer2 += dt;
                
                if (velocity.y < -9.9f) // reached terminal velocity
                    data.state = MainState.FreeFall;
                else if (data.state != MainState.FreeFall)
                    data.state = MainState.Fall;
                return;
            }
            // there's ground
            data.fallTimer2 = 0; 
            if (data.state == MainState.FreeFall)
            {
                data.state = MainState.Dead; data.deadCountdown2 = Timers.FALLDEAD_TIME; return; // die of impact
            }
            
            if(slope==1) // begin sliding when reached unclimbable ground
            {
                data.state = MainState.Slide; return;
            }
            if(data.state == MainState.Slide)
            {
                if (slope > 0) return; // stay in slide until reaching level ground
                data.state = MainState.Normal;
            }
            
            if (data.state != MainState.Normal && data.state != MainState.Fall && data.state != MainState.Climb)
                Debug.LogError($"Wrong state calculation {data.state}"); // debug for now to see if something unexpected
            data.state = MainState.Normal;


        }
        public static void PlayHurt(ref NoodleState data)
        {
            data.state = MainState.Hurt; data.hurtCountdown2 = Timers.PLAYDEAD_TIME;
        }
        public static void PlayDead(ref NoodleState data)
        {
            data.state = MainState.Dead; data.deadCountdown2 = Timers.PLAYDEAD_TIME;
        }

        private static void PlayWakeUp(ref NoodleState data, bool isProne, in NoodleAnimationDatabase animationDB)
        {
            var animation = isProne ? animationDB.wakeUpProne : animationDB.wakeUpSupine;
            PlayEmote(ref data, animation, animation.duration - Timers.WAKEUP_CANCELABLE_TIME);
            //data.emoteTimer = 0; data.emoteState = EmoteState.Playing;
        }
        private static void PlayIdle(ref NoodleState state, in NoodleAnimationDatabase db)
        {
            PlayEmote(ref state, db.idleAnimations[state.random.NextInt(db.idleAnimations.Length)]);
            state.emoteIsIdle = true;
        }

        public static void PlayEmote(ref NoodleState data, in NativeAnimation animation, float inputBlock=-1)
        {

            PlayEmote(ref data, new Emote()
            {
                animation = animation,
                inputBlockingTime = inputBlock,
                type = EmoteType.FullBody
            });
        }

        public static void PlayEmote(ref NoodleState data, in Emote emote)
        {
            //if (data.state != MainState.Normal) return;
            data.emote = emote; data.emoteTimer = 0; data.emoteState = EmoteState.Playing; data.emoteIsIdle = false;
        }
        private static void CancelEmote(ref NoodleState state)
        {
            state.emoteState = EmoteState.Cancelled;
            state.emoteCancelTimer = 0;
        }
        public static void CalculateEmotes(ref NoodleState state, float dt, in NoodleAnimationDatabase db)
        {

            if (state.emoteState == EmoteState.Cancelled)
            {
                state.emoteCancelTimer += dt;
                state.emoteTimer += dt;
                if (state.emoteTimer > state.emote.animation.duration)
                    state.emoteState = EmoteState.None;
            }
            if (state.emoteState == EmoteState.Playing)
            {
                state.emoteTimer += dt;
                if (state.emoteTimer > state.emote.animation.duration)
                    state.emoteState = EmoteState.None;

                // cancel full body emote when left normal state
                if (state.emote.type == EmoteType.FullBody && state.state != MainState.Normal && state.state != MainState.WakeUp)
                    CancelEmote(ref state);

                // cancel upper body emote 
                if (state.emote.type == EmoteType.UpperBody && state.state != MainState.Normal && state.state != MainState.Jump && state.state != MainState.Fall)
                    CancelEmote(ref state);
            }

        }

        public static void ProcessInput(ref NoodleState state, ref InputFrame inputFrame,  in NoodleAnimationDatabase db)
        {
            var cameraLook = new float2(inputFrame.lookPitch, inputFrame.lookYaw);
            var cameraChanged = math.any(cameraLook != state.lastCameraLook);
            if (inputFrame.playDead) NoodleStates.PlayDead(ref state);
            if (inputFrame.playEmote0) NoodleStates.PlayHurt(ref state); //TEMPORARY

            if (state.state == MainState.Dead)
                ClearInput(ref state, ref inputFrame, InputType.All);
            if(state.state == MainState.Hurt)
                ClearInput(ref state, ref inputFrame, InputType.AllButGrab);

            if (state.emoteState!=EmoteState.None) // if emote already playing ignore
                ClearInput(ref state, ref inputFrame, InputType.Emote);

            var playEmote = inputFrame.playEmote0 || inputFrame.playEmote1 || inputFrame.playEmote2 || inputFrame.playEmote3;
            if(playEmote)
            {
                var emote =
                    inputFrame.playEmote0 ? db.emote0 :
                    inputFrame.playEmote1 ? db.emote1 :
                    inputFrame.playEmote2 ? db.emote2 :
                    inputFrame.playEmote3 ? db.emote3 : default;

                if (emote.type == EmoteType.FullBody && state.state == MainState.Normal)
                    PlayEmote(ref state, emote);
                if (emote.type == EmoteType.UpperBody && state.state == MainState.Normal || state.state == MainState.Jump || state.state == MainState.Fall)
                    PlayEmote(ref state, emote);
            }

            // emote playing ...
            if (state.emoteState == EmoteState.Playing) 
            {
                // ... and blocking inputs (full body clear all, upper body blocks grab), clear inputs
                if (state.emoteTimer <= state.emote.inputBlockingTime)
                {
                    if (state.emote.type == EmoteType.FullBody)
                        ClearInput(ref state, ref inputFrame, InputType.All);
                    else
                        ClearInput(ref state, ref inputFrame, InputType.Grab);
                }
                else  // .. past input block and input present, cancel emote
                {
                    if (state.emoteIsIdle && (HasInput(inputFrame, InputType.All)||cameraChanged)) // idle emotes get canceled on camera move
                        CancelEmote(ref state);
                    if (state.emote.type == EmoteType.FullBody && HasInput(inputFrame, InputType.All))
                        CancelEmote(ref state);
                    else if (HasInput(inputFrame, InputType.Grab))
                        CancelEmote(ref state);
                }
            }

            if(inputFrame.jump)
                state.jumpFrames = Timers.JUMP_GRACE_FRAMES;

            // idle animation when no input
            if (state.nextIdleAnimationSchedule == 0)
                state.nextIdleAnimationSchedule = state.random.NextFloat(Timers.IDLE_ANIMATION_MIN, Timers.IDLE_ANIMATION_MAX);
            
            if (state.state == MainState.Normal && state.emoteState == EmoteState.None && !HasInput(inputFrame, InputType.All) && !cameraChanged)
            {
                state.idleTimeWithoutEmotes += World.main.dt;
                if (state.idleTimeWithoutEmotes > state.nextIdleAnimationSchedule)
                {
                    PlayIdle(ref state, db);
                    state.nextIdleAnimationSchedule = 0;
                }
            }
            else
                state.idleTimeWithoutEmotes = 0;
            state.lastCameraLook = cameraLook;

        }


        [Flags]
        public enum InputType
        {
            Move=1,
            Jump=2,
            Grab=4,
            Emote=8,
            AllButGrab = Move | Jump | Emote,
            All = Move|Jump|Emote | Grab
        }

        private static bool HasInput(InputFrame inputFrame, InputType clr = InputType.All)
        {
            return 
                clr.HasFlag(InputType.Move) && inputFrame.moveMagnitude > 0 ||
                clr.HasFlag(InputType.Jump) && inputFrame.jump  ||
                clr.HasFlag(InputType.Grab) && (inputFrame.grabL || inputFrame.grabR ) ||
                clr.HasFlag(InputType.Emote) && (
                    inputFrame.playEmote0 || inputFrame.playEmote1 || inputFrame.playEmote2 || inputFrame.playEmote3);
        }

        private static void ClearInput(ref NoodleState state, ref InputFrame inputFrame, InputType clr = InputType.All)
        {
            if(clr.HasFlag(InputType.Move))
                inputFrame.moveMagnitude = 0;
            if (clr.HasFlag(InputType.Jump))
            {
                inputFrame.jump = false;
                state.jumpFrames = 0;
            }
        if (clr.HasFlag(InputType.Grab))
                inputFrame.grabL = inputFrame.grabR = false;
            if (clr.HasFlag(InputType.Emote))
                inputFrame.playEmote0 = inputFrame.playEmote1 = inputFrame.playEmote2 = inputFrame.playEmote3 = false;
        }

        public static bool CalculateJump(ref NoodleState data)
        {
            if (data.state == MainState.Normal && data.jumpFrames > 0)
            {
                data.state = MainState.Jump; data.jumpTimer2 = 0; data.jumpFrames = 0;
                return true;
            }
            else
            {
                data.jumpFrames--;
                return false;
            }
        }
    }
}