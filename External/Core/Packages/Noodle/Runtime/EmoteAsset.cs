using Noodles.Animation;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public enum EmoteType
    {
        FullBody,
        UpperBody
    }

    [CreateAssetMenu(fileName = "Emote", menuName = "Noodle/Create emote", order = 1)]
    [System.Serializable]
    public class EmoteAsset : ScriptableObject
    {
        public PhysicalAnimation animation;
        public EmoteType type;
        public float inputBlockingTime = 0.0f;
        public bool requiresGroundedPlayer = false;

        public Emote Create(in NoodleDimensions dim)
        {
            return new Emote
            {
                animation = NativeAnimation.Create(animation, dim, loop: false),
                type = type,
                inputBlockingTime = inputBlockingTime,
                requiresGroundedPlayer = requiresGroundedPlayer
            };
        }
    }

    public struct Emote
    {

        public NativeAnimation animation;
        public EmoteType type;
        public float transitionIn => .2f;
        public float transitionOut => .2f;

        public float inputBlockingTime;
        public bool requiresGroundedPlayer;
     
        internal void Dispose()
        {
            animation.Dispose();
        }
        public float GetWeight(EmoteState state, float emoteTimer, float cancelTimer) =>
            GetWeight(state, emoteTimer, cancelTimer, transitionOut);
        public float GetWeight(EmoteState state, float emoteTimer, float cancelTimer, float transitionOut)
        {
            if (state == EmoteState.None) return 0;
            var cancelTime = .25f;
            var weight = 1f;
            if (transitionIn>0 && emoteTimer < transitionIn)
                weight = math.saturate(emoteTimer / transitionIn);
            var emoteLeft = animation.duration - emoteTimer;
            if (transitionOut > 0 && emoteLeft < transitionOut)
                weight = math.saturate(emoteLeft / transitionOut);
            if (state == EmoteState.Cancelled)
                weight *= 1 - math.saturate(cancelTimer / cancelTime);
            return weight;
        }
    }
}
