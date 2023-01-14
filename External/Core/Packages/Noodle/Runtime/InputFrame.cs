using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public struct InputFrame
    {
        public const float PITCH_RANGE = 85 * math.PI / 180;

        public float lookPitch;
        public float lookYaw;
        public float lookPitchVelocity;
        public float lookYawVelocity;
        public float moveYaw;
        public float moveMagnitude;
        public bool grabL;
        public bool grabR;
        public bool jump;
        public bool playDead;
        public bool playEmote0, playEmote1, playEmote2, playEmote3;

        public float lookPitchNormalized => lookPitch / PITCH_RANGE;
        public float lookPitch01 => (lookPitch / PITCH_RANGE + 1) / 2;
        public static float PitchTo01(float pitch) => (pitch / PITCH_RANGE + 1) / 2;
        public static float NormalizePitch(float pitch) => pitch / PITCH_RANGE;

        public float3 forward => moveMagnitude != 0 ? re.forward.RotateY(moveYaw + lookYaw) : float3.zero;

    }
}