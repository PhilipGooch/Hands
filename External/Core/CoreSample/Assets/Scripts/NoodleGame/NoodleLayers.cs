using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Noodles
{
    public enum NoodleLayers
    {

        Default = 1 << 0,
        TransparentFX = 1 << 1,
        IgnoreRaycast = 1 << 2,
        Layer3 = 1 << 3,
        Water = 1 << 4,
        UI = 1 << 5,
        Layer6 = 1 << 6,
        Layer7 = 1 << 7,

        Ball = 1 << 8,
        Player = 1 << 9,
        DefaultNoCam = 1 << 10, // no camera collision
        CollideBall = 1 << 11, // just ball collision (also limits camera)
        CollideBallNoCam = 1 << 12, // just ball collision
        CollidePlayer = 1 << 13, // just player collision (also limits camera)
        CollidePlayerNoCam = 1 << 14, // just player collision
        Triggers = 1 << 15,
        BlockHuman = 1 << 16,
        Listener = 1 << 17,
        GrabTarget = 1 << 18,
        GrabTargetNoCam = 1 << 19,
        FlammableNode = 1 << 26,
        NoOtherCollision = 1 << 25,
        IgnoreHuman = 1 << 23,
        IgnoreLiquid = 1 << 29,
        Slippery = 1 << 30,

        All = -1 & ~NoOtherCollision,
        AllPhysical = All & ~TransparentFX & ~IgnoreRaycast & ~Water & ~UI & ~Listener & ~FlammableNode,
        CollideWithBall = AllPhysical & ~Ball & ~CollidePlayer & ~CollidePlayerNoCam,
        CollideWithPlayer = AllPhysical & ~CollideBall & ~CollideBallNoCam & ~Triggers,
        CollideWithCamera = AllPhysical & ~DefaultNoCam & ~CollideBallNoCam & ~CollidePlayerNoCam & ~GrabTargetNoCam & ~Triggers & ~Player & ~Ball & ~FlammableNode & ~IgnoreHuman,
        GrabTargets = GrabTarget | GrabTargetNoCam,
        Grabbable = AllPhysical & ~Ball
    }


}
