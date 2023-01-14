using NBG.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles.Animation
{
    [System.Serializable]
    public class RingAnimationData
    {
        public PhysicalAnimation N, NE, E, SE, S, SW, W, NW;
    }

    public struct RingAnimation
    {
        public UnsafeArray<NativeAnimation> sections;

        const float sectionAngle = (Mathf.PI * 2.0f) / 8.0f;

        public RingAnimation(RingAnimationData data, in NoodleDimensions dim)
        {
            sections = new UnsafeArray<NativeAnimation>(8, Unity.Collections.Allocator.Persistent);

            sections[0] = NativeAnimation.Create(data.N, dim, loop: true);
            sections[1] = NativeAnimation.Create(data.NE, dim, loop: true);
            sections[2] = NativeAnimation.Create(data.E, dim, loop: true);
            sections[3] = NativeAnimation.Create(data.SE, dim, loop: true);
            sections[4] = NativeAnimation.Create(data.S, dim, loop: true);
            sections[5] = NativeAnimation.Create(data.SW, dim, loop: true);
            sections[6] = NativeAnimation.Create(data.W, dim, loop: true);
            sections[7] = NativeAnimation.Create(data.NW, dim, loop: true);
        }
        public NoodlePose Blend01(float x, float y, float time, in NoodleDimensions dim)
        {
            float angle = CalculateAngle(x, y);
            int sectionIndex = (int)(angle / sectionAngle) % 8;
            float progress = (angle % sectionAngle) / sectionAngle;

            int nextSectionIndex = sectionIndex == 7 ? 0 : sectionIndex + 1;

            var A = sections[sectionIndex].GetPose01(time);
            var B = sections[nextSectionIndex].GetPose01(time);

            return NoodlePose.Blend(A, B, progress, progress, progress, progress, progress, progress, true, true);

        }
        public NoodlePose Blend(float x, float y, float time, in NoodleDimensions dim)
        {
            float angle = CalculateAngle(x, y);
            int sectionIndex = (int)(angle / sectionAngle)%8;
            float progress = (angle % sectionAngle) / sectionAngle;

            int nextSectionIndex = sectionIndex == 7 ? 0 : sectionIndex + 1;

            var A = sections[sectionIndex].GetPose(time, loop: true);
            var B = sections[nextSectionIndex].GetPose(time, loop: true);

            return NoodlePose.Blend(A, B, progress, progress, progress, progress, progress, progress, true, true);
        }

        private float CalculateAngle(float x, float y)
        {
            return math.atan2(-x, -y) + Mathf.PI;
        }

        public void Dispose()
        {
            for (int i = 0; i < 8; i++)
                sections[i].Dispose();
        }
    }
}
