using NBG.Core.Easing;
using Noodles;
using System;

namespace Noodles.Animation
{
    public enum EasingType
    {
        Default,
        step,
        linear,
        sineIn,
        sineOut,
        quadIn,
        quadOut,
        cubicIn,
        cubicOut,
        circleIn,
        circleOut,
        sineInOut,
        quadInOut,
        cubicInOut,
        circleInOut,
    }

    public static class Blending
    {
        public static float Easing(float prev, float next, EasingType easingType, EasingType defaultEasing, float progress)
        {
            switch (easingType)
            {
                case EasingType.Default:
                    if (defaultEasing == EasingType.Default)
                    {
                        return prev;
                    }
                    return Easing(prev, next, defaultEasing, defaultEasing, progress);
                case EasingType.step:
                    return prev;
                case EasingType.linear:
                    return Ease.linear(prev, next, progress);
                case EasingType.sineIn:
                    return Ease.easeInSine(prev, next, progress);
                case EasingType.sineOut:
                    return Ease.easeOutSine(prev, next, progress);
                case EasingType.quadIn:
                    return Ease.easeInQuad(prev, next, progress);
                case EasingType.quadOut:
                    return Ease.easeOutQuad(prev, next, progress);
                case EasingType.cubicIn:
                    return Ease.easeInCubic(prev, next, progress);
                case EasingType.cubicOut:
                    return Ease.easeOutCubic(prev, next, progress);
                case EasingType.circleIn:
                    return Ease.easeInCirc(prev, next, progress);
                case EasingType.circleOut:
                    return Ease.easeOutCirc(prev, next, progress);
                case EasingType.sineInOut:
                    return Ease.easeInOutSine(prev, next, progress);
                case EasingType.quadInOut:
                    return Ease.easeInOutQuad(prev, next, progress);
                case EasingType.cubicInOut:
                    return Ease.easeInOutCubic(prev, next, progress);
                case EasingType.circleInOut:
                    return Ease.easeInOutCirc(prev, next, progress);
                default:
                    return prev;
            }
        }
    }   
}
