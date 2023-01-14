using NBG.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    [Serializable]
    public struct FloatTrajectory
    {
        public float from;
        public float to;
        public EaseType ease;

        public FloatTrajectory(float from, float to, EaseType ease = EaseType.linear)
        {
            this.from = from;
            this.to = to;
            this.ease = ease;
        }

        public float Sample(float mix) => Ease.EasingFromType(from, to, mix, ease);

        public static FloatTrajectory Lerp(FloatTrajectory a, FloatTrajectory b, float mix)
        {
            return new FloatTrajectory(
                math.lerp(a.from, b.from, mix),
                math.lerp(a.to, b.to, mix),
                a.ease);
        }

        public FloatTrajectory ToRadians()
        {
            return new FloatTrajectory(math.radians(from), math.radians(to), ease);
        }
        public static FloatTrajectory operator- (FloatTrajectory a)
        {
            return new FloatTrajectory(-a.from, -a.to, a.ease);
        }
        public static implicit operator FloatTrajectory(float value)
        {
            return new FloatTrajectory(value, value);
        }
    }
    [Serializable]
    public struct Float3Trajectory
    {
        public FloatTrajectory x;
        public FloatTrajectory y;
        public FloatTrajectory z;

        public Float3Trajectory(FloatTrajectory x, FloatTrajectory y, FloatTrajectory z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public float3 Sample(float mix)
        {
            return new float3(x.Sample(mix), y.Sample(mix), z.Sample(mix));
        }
        public Float3Trajectory ToRadians()
        {
            return new Float3Trajectory(x.ToRadians(), y.ToRadians(), z.ToRadians());
        }
    }
    
}
