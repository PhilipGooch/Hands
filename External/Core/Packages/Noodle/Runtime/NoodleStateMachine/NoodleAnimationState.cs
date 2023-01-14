using Noodles.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noodles
{
    //public enum AnimationStateType
    //{
    //    Progression,
    //    Loop
    //}

    //public struct NoodleAnimationState
    //{
    //    public AnimationStateType type;
    //    public NativeAnimation animation;
    //    public float time;

    //    //public NoodleAnimationState(PhysicalAnimation physicalAnimation, in NoodleDimensions dim, AnimationStateType type) 
    //    //{
    //    //    this.type = type;
    //    //    animation = NativeAnimation.Create(physicalAnimation, dim, loop: type==AnimationStateType.Progression);
    //    //    time = 0.0f;
    //    //}

    //    public NoodlePose GetPose(float pitch)
    //    {
    //        switch (type)
    //        {
    //            case AnimationStateType.Progression:
    //                return animation.GetPose01(pitch);
    //            case AnimationStateType.Loop:
    //                time += 1.0f;

    //                if (time > animation.frameLength)
    //                {
    //                    time -= animation.frameLength;
    //                }
    //                return animation.GetPose(time);
    //        }

    //        return NoodlePose.defaultPose;
    //    }
    //}
}
