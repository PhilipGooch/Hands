using System;
using NBG.Core.GameSystems;
using NBG.Core.Streams;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Net.Systems
{
    public interface INetFrameReader
    {
        void OnEnable();
        void OnDisable();

        void Read(IStreamReader frame0, IStreamReader frame1, float mix, float timeBetweenFrames);
        void AddDelta(IStreamReader baseStream, IStreamReader deltaStream, IStreamWriter targetStream);
    }

    /// <summary>
    /// Parses the master stream (container) and distributes the inner streams for parsing by the registered readers.
    /// </summary>
    //TODO: Verify that this runs in the correct group and order.
    [UpdateInGroup(typeof(LateUpdateSystemGroup), OrderFirst = true)] 
    public class NetReadAndApplyFrame : GameSystem
    {
        private const int MaxFrameDelay = 10; //Maximum of Fixed Update delay we tolerate. If exceeded we fast forward immediately. This is always lag recovery
        private const int TargetFrameDelay = 5; //Preferred Frame Distance. Smaller values lead to more jitter, larger values to more delay/sluggishness.

        //Regulate Frame Delay constants
        private const float RenderLagCeiling = 0.3f; //Upper limit for Time.unscaledDeltaTime. This deals with Frame stutter or long render pauses
        private const int FrameDeltaCeiling = 2; //if we are FrameDeltaCeiling away from TargetFrameDelay, we will slow down or speed up time.
        private const float speedUpTime = 1.1f; //The factor we are speeding up Time to catch up with Server
        private const float slowDownTime = 0.9f; //The factor we are slowing down to give the Server more time go generate and send frames.

        private int frameDelay = TargetFrameDelay; //how many Frames we have stockpiled
        private readonly MasterStreamList masterStreams = new MasterStreamList();
        private int lastAppliedFrameID; //The frameID of the frame we rendered last OnUpdate
        private float frameToPhysicsOffset; //time between the last Update and last FixedUpdate
        
        private List<INetFrameReader> readers = new List<INetFrameReader>(); //TODO@!

        private NetEventBus netEventBus; //TODO: convert to reader?

        protected override void OnCreate()
        {
            Enabled = false;

            readers.Add(NetBehaviourList.instance);
        }

        protected override void OnStartRunning()
        {
            netEventBus = World.GetExistingSystem<NetEventBus>();
            Debug.Assert(netEventBus != null);

            for (int i = 0; i < readers.Count; ++i)
            {
                var reader = readers[i];
                reader.OnEnable();
            }
        }

        protected override void OnStopRunning()
        {
            netEventBus = null;

            for (int i = 0; i < readers.Count; ++i)
            {
                var reader = readers[i];
                reader.OnDisable();
            }
        }

        public int InsertStream(IStreamReader newStream)
        {
            var frameID = newStream.ReadFrameId();
            var basedOnID = newStream.ReadFrameId();
            if (basedOnID > 0)
            {
                var deltaStream = newStream.ReadStream();
                masterStreams.InsertDelta(basedOnID, frameID, deltaStream);
            }
            else
            {
                var fullStream = newStream.ReadStream();
                masterStreams.Insert(fullStream,frameID);
            }
            return frameID;
        }

        public void TempProcessNetEventBus(IStreamReader data)
        {
            var frameId = data.ReadFrameId();
            netEventBus.ProcessEventsFromServer(data, frameId);
        }
        /// <summary>
        /// This function slows down or speeds up replay speed, depending on number of Frames stockpiled.
        /// Its default configuration aims to be TargetFrameDelay behind the most recent server Frame.
        /// It also catches up immediately, in Lag recovery cases.
        /// 
        /// NOTE: Servers DO NOT run the same FixedDeltaTime as all clients all the time. Over Time the client may run faster or slower and we need to handle this.
        /// When this happens we either introduce artificial lag, by consuming Frames slower then they are send or we consume
        /// them faster, which introduces jitter.
        /// By slowing down or speeding up replay speed by a small amount, every couple of frames, we keep a smooth distance to the
        /// server's most recent frame, without falling behind or consuming to far.
        ///
        /// If you get one (1 exactly) EVERY (absolutely always EVERY) fixed update TargetFrameDelay can go as low as 2, FrameDeltaCeiling as low as 1.
        /// In practice you will receive bursts of 2-5 packages at once. TargetFrameDelay should be the upper limit value.
        ///
        /// Larger values introduce sluggishness and generally are detrimental to responsiveness.
        /// Smaller values will cause the same Frame being rendered again, which causes jitter, amplified to not being able to extract
        /// forces from just one frame (more jitter in ThirdPersonCamera and Skin, because re.integrate* stops working)
        ///
        /// </summary>
        private void RegulateFrameDelay()
        {
            //Keep delay close to target value
            var mostRecentFrame = masterStreams.GetNewestFrameID();
            var frameTimeVal = math.min(RenderLagCeiling, Time.unscaledDeltaTime); //NOTE: This min prevents long render pauses breaking things.
            var currentFrameDelay = mostRecentFrame - lastAppliedFrameID;
            currentFrameDelay = math.max(currentFrameDelay, 0);

            //We are way to behind -> Catchup to most recent
            if (currentFrameDelay >= MaxFrameDelay)
            {
                lastAppliedFrameID = mostRecentFrame - TargetFrameDelay;
            }
            else
            {
                //keep values close by speeding up/slowing down time
                var deltaToTarget = TargetFrameDelay - currentFrameDelay;
                if (deltaToTarget < FrameDeltaCeiling)
                {
                    //We are behind, speed up time a little
                    frameTimeVal *= speedUpTime;
                }
                else if (deltaToTarget > FrameDeltaCeiling)
                {
                    //We are ahead, slow down a bit
                    frameTimeVal *= slowDownTime;
                }
            }

            //Advance physics frames based on visual frames
            frameToPhysicsOffset += frameTimeVal;
            while (frameToPhysicsOffset > Time.fixedUnscaledDeltaTime * frameDelay && Time.fixedUnscaledDeltaTime > 0)
            {
                frameToPhysicsOffset -= Time.fixedUnscaledDeltaTime;
                lastAppliedFrameID += 1;
            }
        }
        
        protected override void OnUpdate()
        {
            if (masterStreams.IsEmpty)
            {
                //No Frames at all? -> don't apply anything.
                return;
            }
            RegulateFrameDelay();
            //Get the frame to render + next one and calculate mix
            var timePassedSincePhysicsFrame = math.max(frameToPhysicsOffset - (Time.fixedUnscaledDeltaTime * (frameDelay-1)), 0);
            masterStreams.SelectFrameIDsForInterpolation(lastAppliedFrameID, timePassedSincePhysicsFrame , out var frame0, out var frame1, out var mix, out var timeBetweenFrames);
            Debug.Assert(mix >= 0 && mix <= 1);
            //Frames are selected, make sure events are replayed
            netEventBus.EnsureEventsReplayed((int)math.max(frame0, frame1));
            masterStreams.EnsureUnpacked(frame1, Unpack);
            //Get Frames (this will potentially unpack pending delta's and needs to be done after events are replayed so the scene matches this stream)
            if (!masterStreams.TryGet(frame0, out var frame0Stream, out var basedOnID))
            {
                throw new Exception($"Tried to render {frame0} but was not found");
            }
            if (basedOnID > 0)
            {
                throw new Exception($"Frame {frame0} is not unpacked");
            }

            netEventBus.EnsureEventsReplayed(lastAppliedFrameID);

            if (frame0 == frame1)
            {
                ApplyFramesInterpolated(null, frame0Stream, mix, timeBetweenFrames);
            }
            else
            {
                if (!masterStreams.TryGet(frame1, out var frame1Stream, out basedOnID))
                {
                    throw new Exception($"Frame {frame1} is not unpacked");
                }
                
                //Apply object positions and calculate Velocities, etc. All the stuff that ReadState needs to to
                ApplyFramesInterpolated(frame0Stream, frame1Stream, mix, timeBetweenFrames);
            }
        }

        private IStream Unpack(IStream baseStream, IStream deltaStream)
        {
            var targetStream = BasicStream.Allocate(1200);
            for (int i = 0; i < readers.Count; ++i)
            {
                IStream readerStream = BasicStream.Allocate(1024);
                var reader = readers[i];
                reader.AddDelta(baseStream.ReadStream(), deltaStream.ReadStream(), readerStream);
                readerStream.Flip();
                targetStream.WriteStream(readerStream);
            }
            targetStream.Flip();
            baseStream.Seek(0);
            return targetStream;
        }

        private void ApplyFramesInterpolated(IStream frame0, IStream frame1, float mix, float timeBetweenFrames)
        {
            for (int i = 0; i < readers.Count; ++i)
            {
                var reader = readers[i];
                reader.Read(frame0?.ReadStream(), frame1.ReadStream(), mix, timeBetweenFrames);
            }
        }
    }
}
