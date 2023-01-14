using NBG.Unsafe;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Noodles.Animation
{
    [Serializable]
    public struct PhysicalAnimationKeyframe
    {
        public int time;
        public EasingType easeType;
        public float value;
    }

    /// <summary>
    /// Contains frame data that can be modified and accessed by the backend relative to time and frames.
    /// </summary>
    // TODO: setup default transition and default value
    [Serializable]
    public class PhysicalAnimationTrack
    {
        public string name;
        public List<PhysicalAnimationKeyframe> frames;
        [NonSerialized]
        public float defaultValue;
        static EasingType defaultEase => EasingType.step;

        public PhysicalAnimationKeyframe GetKeyAtIndex(int index)
        {
            Unsafe.CheckIndex(index, frames.Count);
            return frames[index];
        }
        public bool TryGetKeyAtIndex(int index, out PhysicalAnimationKeyframe frame)
        {
            if (index < 0 || index >= frames.Count) { frame = default; return false; }
            frame = frames[index];
            return true;
        }

        public bool TryGetKeyIndex(int frameTime, out int index)
        {

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                if (frame.time == frameTime)
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
        public bool KeyExists(int frameTime)
        {
            return TryGetKeyIndex(frameTime, out _);
        }
        public void SetKey(PhysicalAnimationKeyframe frame)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                var existingFrame = frames[i];
                if (existingFrame.time == frame.time)
                {
                    frames[i] = frame;
                    return;
                }
                else if (existingFrame.time > frame.time)
                {
                    frames.Insert(i, frame);
                    return;
                }
            }

            frames.Add(frame);
        }
        public void SetValue(int time, float value)
        {
            if (TryGetKeyIndex(time, out var index))
            {
                var frame = frames[index];
                frame.value = value;
                frames[index] = frame;
            }
            else
            {
                // copy ease type from next frame
                var nextIndex = NextKeyIndex(time, loop: true);
                var easeType = nextIndex < frames.Count ? frames[nextIndex].easeType : defaultEase;
                SetKey(new PhysicalAnimationKeyframe() { time = time, value = value, easeType = easeType });
            }
        }

        public void SetNewTime(int index, int newTime)
        {
            Unsafe.CheckIndex(index, frames.Count);
            if (newTime >= 0)
            {
                var frame = frames[index];
                frames.RemoveAt(index);
                frame.time = newTime;
                SetKey(frame);
            }
        }

        public EasingType GetEasingAt(int time, bool looped)
        {
            int next = NextKeyIndex(time, looped);
            if (TryGetKeyAtIndex(next,out var frame))
            {
                return frame.easeType;
            }
            return EasingType.Default;
        }

        public void SetNewEase(int index, EasingType newEaseType)
        {
            Unsafe.CheckIndex(index, frames.Count);
            var frame = frames[index];
            frame.easeType = newEaseType;
            frames[index] = frame;
        }
        public void DeleteKey(int index)
        {
            Unsafe.CheckIndex(index, frames.Count);
            frames.RemoveAt(index);
        }


        public int PrevKeyIndex(float time, bool loop = false)
        {
            for (int i = frames.Count - 1; i >= 0; i--)
                if (frames[i].time <= time) return i;
            if (loop) return frames.Count - 1;
            return 0;
        }
        public int NextKeyIndex(float time, bool loop = false)
        {
            for (int i = 0; i < frames.Count; i++)
                if (frames[i].time >= time) return i;
            if (loop) return 0;
            return frames.Count - 1;
        }
        public float Sample(float time, bool loop = false, int duration = -1)
        {
            if (loop && duration < 0) throw new InvalidOperationException("Must supply animation duration to wrap");
            if (frames.Count == 0)
                return defaultValue;

            var prevFrame = frames[PrevKeyIndex(time, loop)];
            var nextFrame = frames[NextKeyIndex(time, loop)];

            int prevTime = prevFrame.time;
            int nextTime = nextFrame.time;
            if (loop && nextTime < time) nextTime += duration;
            if (loop && prevTime > time) prevTime -= duration;
            var mix = InverseLerpTime(time, duration, prevTime, nextTime, loop);
            return Blending.Easing(prevFrame.value, nextFrame.value, nextFrame.easeType, EasingType.step, mix);
        }

        public static float InverseLerpTime(float time, int duration, int prevTime, int nextTime, bool loop)
        {
            if (loop && prevTime > time) prevTime -= duration;
            if (loop && nextTime < time) nextTime += duration;
            var mix = re.InverseLerp(prevTime, nextTime, time);
            return mix;
        }

        public override string ToString() => name;
    }

    /// <summary>
    /// Contains the data that represents an animation that can be modified. It can't be played... it's transformed into NativeAnimations (an inmmutable version) when played.
    /// </summary>
    [CreateAssetMenu(fileName = "RecoilAnimation", menuName = "Noodle/Create Recoil Animation", order = 1)]
    [System.Serializable]
    public class PhysicalAnimation : ScriptableObject, ISerializationCallbackReceiver
    {
        public PhysicalAnimation fallback;

        
        [NonSerialized]
        public List<PhysicalAnimationTrack> allTracks;
        [NonSerialized]
        public List<int> groups;

        [FormerlySerializedAs("tracks2")]
        public List<PhysicalAnimationTrack> tracks;

        public bool looped;

        public int frameLength = 100;
        public float playbackSpeed = 1.0f;

        public bool isInitialized => allTracks != null && allTracks.Count > 0;

        public void Initialize(int [] groupSizes, string[] trackNames)
        {
            if (groups == null)
                groups = new List<int>(groupSizes);
            if (allTracks == null)
                allTracks = new List<PhysicalAnimationTrack>();
            for (int i = 0; i < trackNames.Length; i++)
            {
                var found = false;
                for (int t = 0; t < allTracks.Count; t++)
                {
                    if (trackNames[i].Equals(allTracks[t].name))
                    {
                        found = true;
                        if (i != t) // rearrange track
                        {
                            var track = allTracks[t];
                            allTracks.RemoveAt(t);
                            allTracks.Insert(i, track);
                        }
                        break;
                    }
                }
                if (!found)
                {
                    var track = new PhysicalAnimationTrack() { name = trackNames[i], frames = new List<PhysicalAnimationKeyframe>() };
                    allTracks.Insert(i, track);
                }
                allTracks[i].defaultValue=NoodleAnimationLayout.GetDefaultValueByTrackIndex(i);
            }
            while(allTracks.Count>trackNames.Length)
            {
                Debug.LogWarning($"Obsolete track '{allTracks[trackNames.Length].name}' found in animation, re-save to remove it");
                allTracks.RemoveAt(trackNames.Length);
            }
        }

        public float Sample(int track, float time)
        {
            Unsafe.CheckIndex(track, allTracks.Count);
            return allTracks[track].Sample(time, looped, frameLength);
        }

        public void OnBeforeSerialize()
        {
            tracks = new List<PhysicalAnimationTrack>();
            // remove missing tracks to keep animation cleaner
            for (int i = 0; i < allTracks.Count; i++)
                if (allTracks[i].frames.Count > 0)
                    tracks.Add(allTracks[i]);
        }

        public void OnAfterDeserialize()
        {
            if(tracks!=null &&tracks.Count>0)
                allTracks = new List<PhysicalAnimationTrack>(tracks);
            Initialize(NoodleAnimationLayout.ListAnimationGroups(), NoodleAnimationLayout.ListAnimationTracks());
        }

        public unsafe T SampleNoIK<T>(float time, int track, int count, bool loop) where T : unmanaged  // sampling raw frame
        {
            var res = default(T);
            var pRes = (float*)res.AsPointer();
            for (int t = 0; t < count; t++)
                pRes[t] = allTracks[track + t].Sample(time, loop, frameLength);
            return res;
        }

        public PhysicalAnimation ResolveFallbackTracks()
        {
            if (fallback == null) return this;
            var result  = ScriptableObject.Instantiate(this);
            result.fallback = null;
            var resolvedFallback = fallback.ResolveFallbackTracks();

            result.OnAfterDeserialize(); // ensure tracks

            var overrideIndividualTracks = true;
            if (overrideIndividualTracks)
            {
                for (int i = 0; i < allTracks.Count; i++)
                {
                    var track = result.allTracks[i];
                    if (track.frames.Count == 0)
                    {
                        var srcTrack = resolvedFallback.FindTrack(track.name);
                        if (srcTrack != null)
                            track.frames = new List<PhysicalAnimationKeyframe>(srcTrack.frames);
                    }
                }
            }
            else
            {
                var start = 0;
                for (var g = 0; g < result.groups.Count; g++)
                {
                    // check if this group has data
                    bool hasData = false;
                    for (int t = 0; t < result.groups[g]; t++)
                    {
                        var track = result.allTracks[start + t];
                        var thisTrack = FindTrack(track.name);
                        if (thisTrack != null && thisTrack.frames.Count > 0)
                            hasData = true;
                    }
                    // if no data in the group, use tracks from fallback animation instead
                    var src = hasData ? this : resolvedFallback;
                    for (int t = 0; t < result.groups[g]; t++)
                    {
                        var track = result.allTracks[start + t];
                        var thisTrack = src.FindTrack(track.name);
                        if (thisTrack != null)
                            track.frames = new List<PhysicalAnimationKeyframe>(thisTrack.frames);

                    }
                    start += result.groups[g];
                }
            }
            return result;
        }

        private PhysicalAnimationTrack FindTrack(string name)
        {
            for (int i = 0; i < allTracks.Count; i++)
                if (allTracks[i].name.Equals(name))
                    return allTracks[i];
            return null;
        }
    }
}