using NBG.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public interface ILerpStruct<T>
    {
        T Lerp(in T to, float t);
    }

    public struct StructTransition<T> where T : ILerpStruct<T>
    {
        public T current;
        public T old;
        public T target;
        public float time;
        public float duration;
        public EaseType ease;
        public void Step(float dt)
        {
            if (duration == 0)
                current = target;
            else
            {
                time += dt;
                current = old.Lerp(target, Ease.EasingFromType(math.saturate(time / duration), ease));
            }
        }
        public void Transition(T target, float duration, EaseType ease = EaseType.easeOutCirc)
        {
            this.ease = ease;
            this.old = current;
            this.target = target;
            this.duration = duration;
            this.time = 0;
        }
    }
}
