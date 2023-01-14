using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public partial class NoodleAnimator
    {
        const float FULL_CLIMB_DURATION = 1f; // prevents propelling up with too fast controls
        private static void GetPoseClimb(ref NoodleAnimatorData data, ref NoodleState state, in Aim aim, float dt, out NoodlePose pose, in NoodleDimensions dim)
        {
            var climbAim01 = (data.lastClimbPitch01 < 0)? aim.pitch01: // if not set snap to aim
                math.min(aim.pitch01, math.saturate( data.lastClimbPitch01 + dt/ FULL_CLIMB_DURATION)); // otherwise control animation speed
            // pick random animation if was not climbing
            if (data.lastClimbPitch01 < .5f && aim.pitch01 >= .5f) 
                data.climbSelector = state.random.NextInt(4) / 3f;
            data.lastClimbPitch01 = climbAim01;
            

            var climbPose = data.animationDB.climb.GetPose01(climbAim01);
            var flipped = climbPose;
            flipped.Flip();
            climbPose = NoodlePose.Blend(flipped, climbPose, data.climbSelector);


            var swingPose = data.animationDB.swing.GetPose01(aim.pitch01);

            pose =  NoodlePose.Blend(climbPose, swingPose, state.swingBlend);

            // reach with other hand - need firm shoulders
            if (state.handStateL == HandState.Grab || state.handStateR == HandState.Grab)
            {
                pose.torso.angularTonus = 1;
                // help for wall climbing
                pose.torso.chestRoll += state.handStateR == HandState.Grab?math.radians(5): math.radians(-5);
                pose.torso.chestYaw+= state.handStateR == HandState.Grab ? math.radians(-5) : math.radians(5);
            }
            
        }
    }
}
