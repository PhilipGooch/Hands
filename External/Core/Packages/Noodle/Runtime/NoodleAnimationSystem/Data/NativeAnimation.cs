using NBG.Unsafe;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Noodles.Animation
{
    /// <summary>
    /// This struct contains the animation data that can be used in burst/jobs. The data layout speeds up reading but it's not ready to be modified.
    /// </summary>
    public unsafe struct NativeAnimation
    {
        public int frameLength;
        public float duration;

        public int trackCount;
        public UnsafeArray<float> data;


        public const float perFrameTime = 0.02f;

        public static NativeAnimation empty => new NativeAnimation();
        public unsafe bool isEmpty => data.ptr == IntPtr.Zero;

        public static NativeAnimation Create(PhysicalAnimation animation, in NoodleDimensions dim, bool loop)
        {
            var native = new NativeAnimation();
            native.Bake(animation, dim, loop);
            return native;
        }

        int index(int track, int frame)
        {
            var val = frame * trackCount + track;
            Unsafe.CheckIndex(val, data.Length);
            return val;
        }

        public void Bake(PhysicalAnimation animation, in NoodleDimensions dim, bool loop)
        {
            Dispose();
            if (animation == null || animation.frameLength == 0 || animation.allTracks.Count < NoodleAnimationLayout.physicalTrackCount) return;
            var resolved = animation.ResolveFallbackTracks();

            trackCount = NoodleAnimationLayout.nativeTrackCount;
            frameLength = resolved.frameLength;
            duration = resolved.frameLength * NativeAnimation.perFrameTime;

            data = new UnsafeArray<float>(trackCount * (frameLength + 1), Allocator.Persistent);// allow for last frame

            NoodleAnimationLayout.BakeAnimation(resolved, dim, this, loop);

            //isCreated = true;
        }

        public unsafe ref float this[int track, int frame] => ref data.ElementAt(index(track, frame));

        internal unsafe T Sample<T>(float time, ref int track, int count, bool wrap = false) where T : unmanaged
        {
            var res = Sample<T>(time, track, count, wrap);
            track += count;
            return res;
        }
        internal unsafe T Sample<T>(float time, int track, int count, bool wrap = false) where T : unmanaged
        {
            var res = default(T);
            float frameTime = time / perFrameTime;
            var prevFrame = (int)math.floor(frameTime);
            var nextFrame = (int)math.ceil(frameTime);
            var mix = frameTime - prevFrame;
            prevFrame = Clamp(prevFrame, wrap);
            nextFrame = Clamp(nextFrame, wrap);
            var pA = index(track, prevFrame);
            var pB = index(track, nextFrame);
            var pRes = (float*)res.AsPointer();
            while (count-- > 0)
                *pRes++ = math.lerp(data.ElementAt(pA++), data.ElementAt(pB++), mix);

            return res;
        }
        internal unsafe T ReadFrame<T>(int time, ref int track, int count) where T : unmanaged
        {
            var res = ReadFrame<T>(time, track, count);
            track += count;
            return res;
        }
        internal unsafe T ReadFrame<T>(int frame, int track, int count) where T : unmanaged
        {
            var res = default(T);
            var pA = index(track, frame);
            var pRes = (float*)res.AsPointer();
            while (count-- > 0)
                *pRes++ = data.ElementAt(pA++);

            return res;
        }
        internal unsafe ref T GetFrameReference<T>(int frame, int track) where T : unmanaged
        {
            var pA = index(track, frame);
            return ref *(T*)data.ElementAt(pA).AsPointer();
        }
        internal unsafe void WriteFrame<T>(T pose, int frame, int track, int count) where T : unmanaged
        {
            var pA = index(track, frame);
            var pRes = (float*)pose.AsPointer();
            while (count-- > 0)
                data.ElementAt(pA++) = *pRes++;
        }
        public int Clamp(int frame, bool wrap)
        {
            if (wrap)
                return (frame % frameLength + frameLength) % frameLength;

            if (frame < 0) frame = 0;
            if (frame >= frameLength) frame = frameLength;
            return frame;
        }

        internal void OverrideUpperBodyPoseLooped(float time, ref NoodlePose pose)
        {
            OverrideUpperBodyPose(time % (duration), ref pose);
        }

        internal void OverrideUpperBodyPose(float time, ref NoodlePose pose)
        {
            throw new NotImplementedException();
            //float frameTime = time / perFrameTime;
            //pose.head = headTrack.GetPose(frameTime);
            //pose.torso = torsoTrack.GetPose(frameTime);
            //pose.handL = leftArmTrack.GetPose(frameTime);
            //pose.handR = rightArmTrack.GetPose(frameTime);
        }



        public void Dispose()
        {
            if (!isEmpty)
                data.Dispose();
        }
    }
}


