using NBG.Core.Easing;
using Unity.Mathematics;

namespace Noodles
{
    public struct Float3Ease
    {
        public float3 from;
        public float3 to;
        public EaseType ease;
        public float duration;
        public float startTime;
        public float3 Get(float now) => duration > 0 ? math.lerp(from, to, Ease.EasingFromType(math.saturate((now - startTime) / duration), ease)) : to;
        public void TransitionTo(float time, float3 target)
        {
            from = Get(time);
            to = target;
            startTime = time;
        }
        public void TransitionTo(float time, float3 target, float duration)
        {
            TransitionTo(time, target);
            this.duration = duration;
        }
        public void TransitionTo(float time, float3 target, float duration, EaseType ease)
        {
            TransitionTo(time, target);
            this.duration = duration;
            this.ease = ease;
        }

    }
    public struct FloatEase
    {
        public float from;
        public float to;
        public EaseType ease;
        public float duration;
        public float startTime;
        public float Get(float now) => duration > 0 ? math.lerp(from, to, Ease.EasingFromType(math.saturate((now - startTime) / duration), ease)) : to;
        public void TransitionTo(float time, float target)
        {
            from = Get(time);
            to = target;
            startTime = time;
        }
        public void TransitionTo(float time, float target, float duration)
        {
            TransitionTo(time, target);
            this.duration = duration;
        }
        public void TransitionTo(float time, float target, float duration, EaseType ease)
        {
            TransitionTo(time, target);
            this.duration = duration;
            this.ease = ease;
        }

    }

}