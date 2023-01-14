using Recoil;
using Noodles.Animation;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using NBG.Unsafe;
using System;

namespace Noodles
{

    public unsafe struct NativeAnimationGroup<T> where T : unmanaged
    {
        public T defaultPose;
        public int startTrack;
        public int trackCount;

        public NativeAnimationGroup(int startTrack, int trackCount, T defaultPose) : this()
        {
            this.startTrack = startTrack;
            this.trackCount = trackCount;
            this.defaultPose = defaultPose;
        }

        public T GetPose(in NativeAnimation animation, float time, bool loop)
        {
            if (animation.isEmpty) return defaultPose;
            return animation.Sample<T>(time, startTrack, trackCount, loop);
        }
        public T GetPose01(in NativeAnimation animation, float progress)
        {
            float time = progress * animation.duration;
            return GetPose(animation, time, loop: false);
        }
    }

    /// <summary>
    /// This defines the tracks layout that we use for Noodle. The system could have different layouts for other types of rigs like animals... and this is the specific one for Noodle.
    /// </summary>
    
    public static class NoodleAnimationLayout
    {
       

        const int headStart = TorsoPoseLayout.nativeTrackCount;
        const int armLstart = headStart + HeadPoseLayout.nativeTrackCount;
        const int armRstart = armLstart + ArmPoseLayout.nativeTrackCount;
        const int legLstart = armRstart + ArmPoseLayout.nativeTrackCount;
        const int legRstart = legLstart + LegPoseLayout.nativeTrackCount;
        const int pivotStart = legRstart + LegPoseLayout.nativeTrackCount;
        const int pivotRStart = pivotStart + PivotPoseLayout.nativeTrackCount;
        public readonly static NativeAnimationGroup<TorsoPose> torso = new NativeAnimationGroup<TorsoPose>(0, TorsoPoseLayout.nativeTrackCount, TorsoPose.defaultPose);
        public readonly static NativeAnimationGroup<HeadPose> head = new NativeAnimationGroup<HeadPose>(headStart, HeadPoseLayout.nativeTrackCount, HeadPose.defaultPose);
        public readonly static NativeAnimationGroup<HandPose> armL = new NativeAnimationGroup<HandPose>(armLstart, ArmPoseLayout.nativeTrackCount, HandPose.defaultPose);
        public readonly static NativeAnimationGroup<HandPose> armR = new NativeAnimationGroup<HandPose>(armRstart, ArmPoseLayout.nativeTrackCount, HandPose.defaultPose);
        public readonly static NativeAnimationGroup<LegPose> legL = new NativeAnimationGroup<LegPose>(legLstart, LegPoseLayout.nativeTrackCount, LegPose.defaultPose);
        public readonly static NativeAnimationGroup<LegPose> legR = new NativeAnimationGroup<LegPose>(legRstart, LegPoseLayout.nativeTrackCount, LegPose.defaultPose);
        public readonly static NativeAnimationGroup<PivotPose> pivotL = new NativeAnimationGroup<PivotPose>(pivotStart, PivotPoseLayout.nativeTrackCount, PivotPose.defaultPose);
        public readonly static NativeAnimationGroup<PivotPose> pivotR = new NativeAnimationGroup<PivotPose>(pivotRStart, PivotPoseLayout.nativeTrackCount, PivotPose.defaultPose);

        public static int physicalTrackCount =>
            TorsoPoseLayout.physicalTrackCount
            + HeadPoseLayout.physicalTrackCount
            + ArmPoseLayout.physicalTrackCount * 2
            + LegPoseLayout.physicalTrackCount * 2
            + PivotPoseLayout.physicalTrackCount;
        public static int nativeTrackCount =>
            TorsoPoseLayout.nativeTrackCount
            + HeadPoseLayout.nativeTrackCount
            + ArmPoseLayout.nativeTrackCount * 2
            + LegPoseLayout.nativeTrackCount * 2
            + PivotPoseLayout.nativeTrackCount * 2;
        public static int[] ListAnimationGroups()
        {
            return new int[] {
                TorsoPoseLayout.nativeTrackCount,
                HeadPoseLayout.nativeTrackCount,
                ArmPoseLayout.nativeTrackCount,
                ArmPoseLayout.nativeTrackCount,
                LegPoseLayout.nativeTrackCount,
                LegPoseLayout.nativeTrackCount,
                PivotPoseLayout.nativeTrackCount
            };
        }
        public static string[] ListAnimationTracks()
        {
            var list = new List<string>();
            list.AddRange(TorsoPoseLayout.ListAnimationTracks("torso."));
            list.AddRange(HeadPoseLayout.ListAnimationTracks("head."));
            list.AddRange(ArmPoseLayout.ListAnimationTracks("handL."));
            list.AddRange(ArmPoseLayout.ListAnimationTracks("handR."));
            list.AddRange(LegPoseLayout.ListAnimationTracks("legL."));
            list.AddRange(LegPoseLayout.ListAnimationTracks("legR."));
            list.AddRange(PivotPoseLayout.ListAnimationTracks("pivot."));
            return list.ToArray();
        }

        public static NoodlePose GetPose01(this NativeAnimation animation, float progress)
            => SampleNativeAnimation(animation, progress * animation.duration);
        public static NoodlePose GetPose(this NativeAnimation animation, float time, bool loop = false)
            => SampleNativeAnimation(animation, time, loop);
       
        public unsafe static float GetDefaultValueByTrackIndex(int trackIndex) {
            var p = NoodlePose.defaultPose;
            p.ConvertFromRuntime();
            return ((float*)p.AsPointer())[trackIndex];
        }

        public static NoodlePose SamplePhysicalAnimationNoIK(this PhysicalAnimation animation, float time, bool loop = false)
        {
            return new NoodlePose()
            {
                torso = animation.SampleNoIK<TorsoPose>(time, 0, TorsoPoseLayout.nativeTrackCount, loop).ToRadians(),
                head = animation.SampleNoIK<HeadPose>(time, headStart, HeadPoseLayout.nativeTrackCount, loop).ToRadians(),
                handL = animation.SampleNoIK<HandPose>(time, armLstart, ArmPoseLayout.nativeTrackCount, loop).ToRadians().flipped,
                handR = animation.SampleNoIK<HandPose>(time, armRstart, ArmPoseLayout.nativeTrackCount, loop).ToRadians(),
                legL = animation.SampleNoIK<LegPose>(time, legLstart, LegPoseLayout.nativeTrackCount, loop).ToRadians().flipped,
                legR = animation.SampleNoIK<LegPose>(time, legRstart, LegPoseLayout.nativeTrackCount, loop).ToRadians(),
                pivotL = default,
                pivotR = animation.SampleNoIK<PivotPose>(time, pivotStart, PivotPoseLayout.nativeTrackCount, loop).ToRadians(),
            };
        }

        public static NoodlePose SampleNativeAnimation(this NativeAnimation animation, float time, bool loop = false)
        {
            if (animation.isEmpty) return NoodlePose.defaultPose;
            return animation.Sample<NoodlePose>(time, 0, nativeTrackCount, loop);// directly noodle pose
        }

        public static void BakeAnimation(PhysicalAnimation src, in NoodleDimensions dim, NativeAnimation nativeAnimation, bool loop)
        {
            for (int frame = 0; frame <= nativeAnimation.frameLength; frame++)
            {
                var pose = src.SamplePhysicalAnimationNoIK(frame, loop);
                if (NoodleIK.ikMode.HasFlag(NoodleIKMode.Baked))
                    NoodleIKBurst.SolveInverseKinematics(ref pose, dim, recalculateIK:false); // TODO check if needed
                nativeAnimation.GetFrameReference<NoodlePose>(frame, 0) = pose;
            }

        }
        [BurstCompile]
        public struct NoodleIKBurst
        {
            public unsafe delegate void SolveInverseKinematicsDelegate(NoodlePose* pose, NoodleDimensions* dim, bool recalculateIK);
            public unsafe readonly static SolveInverseKinematicsDelegate ptrSolveInverseKinematicsBurst = BurstCompiler.CompileFunctionPointer<SolveInverseKinematicsDelegate>(SolveInverseKinematicsBurstImpl).Invoke;
            [BurstCompile]
            private unsafe static void SolveInverseKinematicsBurstImpl(NoodlePose* pose, NoodleDimensions* dim, bool recalculateIK)
            {
                NoodleIK.SolveInverseKinematics(ref *pose, *dim, recalculateIK, RigidTransform.identity);
            }

            public unsafe static void SolveInverseKinematics(ref NoodlePose pose, NoodleDimensions dim, bool recalculateIK)
            {
                ptrSolveInverseKinematicsBurst(pose.AsPointer(), dim.AsPointer(), recalculateIK);
            }
        }
        static class TorsoPoseLayout
        {
            public const int nativeTrackCount = 15;
            public const int physicalTrackCount = 15;
            public static string[] ListAnimationTracks(string context)
            {
                return new string[]
                {
                context+nameof(TorsoPose.cg)+".x",
                context+nameof(TorsoPose.cg)+".y",
                context+nameof(TorsoPose.cg)+".z",
                context+nameof(TorsoPose.hipsPitch),
                context+nameof(TorsoPose.hipsYaw),
                context+nameof(TorsoPose.hipsRoll),
                context+nameof(TorsoPose.waistPitch),
                context+nameof(TorsoPose.waistYaw),
                context+nameof(TorsoPose.waistRoll),
                context+nameof(TorsoPose.chestPitch),
                context+nameof(TorsoPose.chestYaw),
                context+nameof(TorsoPose.chestRoll),
                context+nameof(TorsoPose.suspensionTonus),
                context+nameof(TorsoPose.angularTonus),
                context+nameof(TorsoPose.tonus),


                };
            }
        }
        static class HeadPoseLayout
        {
            public const int nativeTrackCount = 5;
            public const int physicalTrackCount = 5;
            public static string[] ListAnimationTracks(string context)
            {
                return new string[]
                {
                    context+nameof(HeadPose.pitch),
                    context+nameof(HeadPose.yaw),
                    context+nameof(HeadPose.roll),
                    context+nameof(HeadPose.fkParent),
                    context+nameof(HeadPose.tonus),
                };
            }
        }
        static class PivotPoseLayout
        {
            public const int nativeTrackCount = 9;
            public const int physicalTrackCount = 9;
            public static string[] ListAnimationTracks(string context)
            {
                return new string[]
                {
                    context+nameof(PivotPose.pitch),
                    context+nameof(PivotPose.yaw),
                    context+nameof(PivotPose.roll),
                    context+nameof(PivotPose.offset)+".x",
                    context+nameof(PivotPose.offset)+".y",
                    context+nameof(PivotPose.offset)+".z",
                    context+nameof(PivotPose.anchor),
                    context+nameof(PivotPose.fkParent),
                    context+nameof(PivotPose.ikBlend),
                };
            }
        }
        static class ArmPoseLayout
        {
            public const int nativeTrackCount = 17;
            public const int physicalTrackCount = 17;
            public static string[] ListAnimationTracks(string context)
            {
                return new string[]
                {
                context+nameof(HandPose.pitch),
                context+nameof(HandPose.yaw),
                context+nameof(HandPose.bend),
                context+nameof(HandPose.elbowAngle),
                context+nameof(HandPose.wristAngle),
                context+nameof(HandPose.fkParent),

                context+nameof(HandPose.ikPos)+".x",
                context+nameof(HandPose.ikPos)+".y",
                context+nameof(HandPose.ikPos)+".z",
                context+nameof(HandPose.ikPosRelative)+".x",
                context+nameof(HandPose.ikPosRelative)+".y",
                context+nameof(HandPose.ikPosRelative)+".z",
                context+nameof(HandPose.ikParent),
                context+nameof(HandPose.ikBlend),

                context+nameof(HandMuscle.upperTonus),
                context+nameof(HandMuscle.lowerTonus),
                context+nameof(HandMuscle.ikDrive),
                };
            }

        }

        static class LegPoseLayout
        {
            public const int nativeTrackCount = 16;
            public const int physicalTrackCount = 16;
            public static string[] ListAnimationTracks(string context)
            {
                return new string[]
                {
                context+nameof(LegPose.pitch),
                context+nameof(LegPose.stretch),
                context+nameof(LegPose.bend),
                context+nameof(LegPose.twist),
                context+nameof(LegPose.fkParent),

                context+nameof(LegPose.ikPos)+".x",
                context+nameof(LegPose.ikPos)+".y",
                context+nameof(LegPose.ikPos)+".z",
                context+nameof(LegPose.ikPosRelative)+".x",
                context+nameof(LegPose.ikPosRelative)+".y",
                context+nameof(LegPose.ikPosRelative)+".z",
                context+nameof(LegPose.ikParent),
                context+nameof(LegPose.ikBlend),

                context+nameof(LegPose.upperTonus),
                context+nameof(LegPose.lowerTonus),
                context+nameof(LegPose.ikDrive),
                };
            }

        }

        public static string GetLabelForIKSync(int track)
        {
            if (track < armLstart || track >= pivotStart) return null;
            string getname(int offset, int fkTracks)
            {
                if (offset < fkTracks) return "FK";
                if (offset < fkTracks + 3) return "IK";
                return "IK Relative";

            }
            if (track >= legRstart) return "legR " + getname(track - legRstart, fkTracks: 5);
            else if (track >= legLstart) return "legL " + getname(track - legLstart, fkTracks: 5);
            else if (track >= armRstart) return "handR " + getname(track - armRstart, fkTracks: 6);
            else if (track >= armLstart) return "handL " + getname(track - armLstart, fkTracks: 6);
            return null;
        }

        public unsafe static bool SyncFKtoIK(PhysicalAnimation src, in NoodleDimensions dim, int track, int frame, SyncIKFKMode mode, out int trackStart, out int trackCount)
        {
            trackStart = trackCount = default;
            if (track < armLstart || track >= pivotStart) return false;

            if (track >= legRstart) SyncFKtoIK(frame, track, src, dim, fkTracks: 5, track: legRstart, srcMode: mode, trackStart: out trackStart, trackCount: out trackCount);
            else if (track >= legLstart) SyncFKtoIK(frame, track, src, dim, fkTracks: 5, track: legLstart, srcMode: mode, trackStart: out trackStart, trackCount: out trackCount);
            else if (track >= armRstart) SyncFKtoIK(frame, track, src, dim, fkTracks: 6, track: armRstart, srcMode: mode, trackStart: out trackStart, trackCount: out trackCount);
            else if (track >= armLstart) SyncFKtoIK(frame, track, src, dim, fkTracks: 6, track: armLstart, srcMode: mode, trackStart: out trackStart, trackCount: out trackCount);

            return true;
        }


        private static unsafe void SyncFKtoIK(int frame, int targetTrack, PhysicalAnimation src, in NoodleDimensions dim, int fkTracks, int track, SyncIKFKMode srcMode, out int trackStart, out int trackCount)
        {
            var p = src.SamplePhysicalAnimationNoIK(frame, src.looped);
            var offset = targetTrack - track;
            var dstMode = offset < fkTracks ? SyncIKFKMode.FK :
                offset < fkTracks + 3 ? SyncIKFKMode.IK : SyncIKFKMode.IKRelative;

            var ptr = ((float*)p.AsPointer());
            ref var ikParent =  ref ptr[track + fkTracks + 6];
            ref var ikBlend= ref ptr[track + fkTracks + 7];
            ikParent = srcMode == SyncIKFKMode.IK ? 0 : 1; // when blending from absolute ik, don't use ikparent
            ikBlend = srcMode == SyncIKFKMode.FK ? 0 : 1; // if blending from fk, don't use ik

            NoodleIKBurst.SolveInverseKinematics(ref p, dim, true);
            p.ConvertFromRuntime();
            trackStart = track + dstMode switch
            {
                SyncIKFKMode.FK => 0,
                SyncIKFKMode.IK => fkTracks,
                SyncIKFKMode.IKRelative => fkTracks + 3,
                _ => throw new NotImplementedException(),
            };
            trackCount = dstMode == SyncIKFKMode.FK ? fkTracks : 3;

            for(int i=0; i<trackCount;i++)
                src.allTracks[trackStart + i].SetValue(frame, ptr[trackStart+i]);
        }


    }

    public enum SyncIKFKMode
    {

        FK,
        IK,
        IKRelative,

    }
}
