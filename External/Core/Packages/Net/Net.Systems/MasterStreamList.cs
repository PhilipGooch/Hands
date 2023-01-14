using System;
using System.Collections.Generic;
using System.Linq;
using NBG.Core.Streams;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Net.Systems
{
    /// <summary>
    /// This class keeps track of NetStreams for buffering, interpolation and Deltas
    /// </summary>
    internal class MasterStreamList
    {
        private readonly int overflowLimit; //This catches overflows by malicious clients. See @overflowLimit in constructor
        private readonly List<Entry> masterStreams = new List<Entry>();
        private readonly Dictionary<INetTransportPeer, int> lastAcks = new Dictionary<INetTransportPeer, int>();
        private readonly CompareByID compareByID = new CompareByID();

        private int keepFrame = 0;

        internal bool IsEmpty => masterStreams.Count <= 0;
        public int Count => masterStreams.Count;
        public int Mem => masterStreams.Sum(x => x.stream.Buffer.Length);

        /// <summary>
        /// Creates a new Master Stream Limit
        /// </summary>
        /// <param name="overflowLimit">Hard Limit to prevent overflows. Use LimitHistory(int lowWaterMark) to clear resources earlier</param>
        internal MasterStreamList(int overflowLimit = 1024)
        {
            this.overflowLimit = overflowLimit;
        }
        /// <summary>
        /// Stores a new Netstream that has been received by the client or created by the Server
        /// </summary>
        /// <param name="masterStream">The streams binary data</param>
        /// <param name="frameID">The frameID this stream was created in</param>
        internal void Insert(IStream masterStream, int frameID)
        {
            Insert(new Entry()
            {
                frameID = frameID,
                stream = masterStream,
            });
        }
        public void InsertDelta(int basedOn, int frameID, IStream deltaStream)
        {
            if (!FindBaseFrame(basedOn))
                throw new Exception($"BaseFrame {basedOn} for frame {frameID} could not be found");
            Insert(new Entry()
            {
                basedOnID = basedOn,
                stream = deltaStream,
                frameID = frameID
            });
            var minimumToKeep = math.min(basedOn, this.keepFrame);
            LimitHistory(minimumToKeep);
        }

        private void Insert(Entry frameStream)
        {
            var pos = masterStreams.BinarySearch(frameStream, compareByID);
            if (pos < 0)
            {
                //New event frame -> Insert at best position for this
                masterStreams.Insert(~pos, frameStream);
            }
            else
            {
                //Happens when a Delta is unpacked to a full stream. 
                masterStreams[pos] = frameStream;
            }
        }
        /// <summary>
        /// Throws away all streams that have been acked by clients or are over the overflow limit
        /// </summary>
        internal void LimitHistoryByAcks()
        {
            if (lastAcks.Count > 0)
            {
                if (masterStreams.Count > overflowLimit)
                {
                    masterStreams.RemoveRange(0, masterStreams.Count - overflowLimit);
                }
                var lowWaterMark = lastAcks.Values.Min(x => x);
                if (lowWaterMark > 0)
                {
                    var entry = new Entry()
                    {
                        frameID = lowWaterMark,
                        stream = null,
                    };
                    var pos = masterStreams.BinarySearch(entry, compareByID);
                    if (pos >= 0)
                    {
                        //Drop all until lowWaterMark, but keep lowWaterMark frame for interpolation
                        var toRemove = pos - 1;
                        if (toRemove > 0)
                        {
                            masterStreams.RemoveRange(0, toRemove);
                        }
                    }
                    else
                    {
                        //low water mark was not exactly found. But we can remove everything up until where it would be
                        var removeCount = (~pos) - 1;
                        if (removeCount > 0)
                        {
                            masterStreams.RemoveRange(0, removeCount);
                        }
                    }
                }
            }
            else
            {
                //Note: AS long as there are no peers, we throw everything away immediately.
                masterStreams.Clear();
            }
        }
        internal bool FindBaseFrame(int baseFrame)
        {
            if(!TryGet(baseFrame, out var _, out int basedOnID))
                return false;
            if (basedOnID > 0)
                return FindBaseFrame(basedOnID);
            return true;
        }
        /// <summary>
        /// Throws away all streams that are older than the low water mark or over the overflow limit.
        /// Prints a warning if a lot of frames will be dropped at once (usually huge lag)
        /// This keeps any Frames that are baseFrames of an unpacked delta
        /// </summary>
        /// <param name="serverBaseFrame">Last Frame you want to keep. Everything older will be cleared, unless its a baseFrame of a delta</param>
        internal void LimitHistory(int serverBaseFrame)
        {
            int dropUntil = serverBaseFrame;
            int dropPoint = 0;
            for (var index = masterStreams.Count - 1; index >= 0; index--)
            {
                var masterStream = masterStreams[index];
                if (masterStream.basedOnID > 0 && masterStream.basedOnID < dropUntil)
                {
                    dropUntil = masterStream.basedOnID;
                }

                if (dropUntil > masterStream.frameID)
                {
                    dropPoint = index;
                    break;
                }
            }

            if (dropPoint > 0)
            {
                masterStreams.RemoveRange(0, dropPoint);
            }
        }

        internal void ClearHistory()
        {
            masterStreams.Clear();
        }

        /// <summary>
        /// Returns a NetStream for a specific full frame or null, if not found 
        /// </summary>
        /// <param name="frameID">The frameID you want the stream for</param>
        /// <param name="stream">The stream of the given FrameID</param>
        /// <param name="basedOnID">0 if a full stream or a valid frameID if this is a delta of another stream and needs to be unpacked</param>
        /// <returns>A INetStream or null</returns>
        internal bool TryGet(int frameID, out IStream stream, out int basedOnID)
        {
            var newEntry = new Entry()
            {
                frameID = frameID,
                stream = null,
            };
            var pos = masterStreams.BinarySearch(newEntry, compareByID);
            if (pos >= 0)
            {
                var entry = masterStreams[pos];

                entry.stream.Seek(0);
                stream = entry.stream;
                basedOnID = entry.basedOnID;
                return true;
            }
            stream = null;
            basedOnID = 0;
            return false;
        }

        private int GetOldestFrameID()
        {
            return masterStreams.First().FrameID;
        }
        /// <summary>
        /// Gets you the most recent frameID, including any pending Deltas
        /// </summary>
        /// <returns>The most recent FrameID</returns>
        public int GetNewestFrameID()
        {
            var ret = masterStreams.Last().FrameID;
            return ret;
        }
        
        internal void SelectFrameIDsForInterpolation(int frame, float timePassedSincePhysicsFrame, out int frame0ID, out int frame1ID, out float mix, out float timeBetweenFrames, bool debug = false)
        {
            keepFrame = frame;
            if (debug) DebugUI.DebugUI.Get().Print($"Getting frame {frame} range is {GetOldestFrameID()} - {GetNewestFrameID()}");
            mix = 0;
            timeBetweenFrames = 0;
            frame1ID = frame0ID = masterStreams.Last().FrameID;

            //Scan backwards to find frame
            for (int i = masterStreams.Count - 1; i >= 0; i--)
            {
                //Found it!
                if (masterStreams[i].FrameID <= frame)
                {
                    frame0ID = masterStreams[i].FrameID;
                    if (i < masterStreams.Count - 1) //Are there any frames after this one
                    {
                        //Return next Frame too
                        frame1ID = masterStreams[i + 1].FrameID;
                        //Calculate mix from fraction: How much time has passed after frame was most recent
                        timeBetweenFrames = ((frame1ID - frame0ID) * Time.fixedDeltaTime);
                        mix = timePassedSincePhysicsFrame / timeBetweenFrames;
                        frame = frame0ID;
                        if (debug) DebugUI.DebugUI.Get().Print($"{Time.frameCount} MIX: {frame} -> {masterStreams[i + 1].FrameID} : {mix:F} : TTF {timeBetweenFrames} : TIF {timePassedSincePhysicsFrame} ");
                    }
                    else
                    {
                        frame1ID = masterStreams[i].FrameID;
                        if (debug) DebugUI.DebugUI.Get().Print($"{Time.frameCount} IS-TOP: {frame}");
                    }

                    return;
                }
            }
        }
       
        /// <summary>
        /// Updates client side stream acks. Used to free stored frames that everybody received
        /// </summary>
        /// <param name="peer">the peer that has send the ack</param>
        /// <param name="frameID">the frameID this peer has acked</param>
        internal void UpdateLastAck(INetTransportPeer peer, int frameID)
        {
            Debug.Assert(frameID > 0, "FrameID 0 is reserved for initital state and cannot be acked");
            if (lastAcks.TryGetValue(peer, out var currentLastAck))
            {
                //Note: in case of out of order packages 
                lastAcks[peer] = math.max(frameID, currentLastAck);
            }
            else
            {
                lastAcks[peer] = frameID;
            }
        }
        /// <summary>
        /// Query the last ack send by a client
        /// </summary>
        /// <param name="peer">The client you are interested in</param>
        /// <returns>-1 if no acks have been received, otherwise FrameID</returns>
        internal int GetLastAck(INetTransportPeer peer)
        {
            if (!lastAcks.TryGetValue(peer, out var currentLastAck))
            {
                return 0;
            }
            return currentLastAck;
        }
        
        internal void RemovePeer(INetTransportPeer peer)
        {
            lastAcks.Remove(peer);
        }
        
        private struct Entry 
        {
            public int frameID;
            public int basedOnID;
            public IStream stream;
            public int FrameID => frameID;
        }

        private class CompareByID : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                return x.FrameID.CompareTo(y.FrameID);
            }
        }

        internal void EnsureUnpacked(int frameID, Func<IStream, IStream, IStream> unpackFunc)
        {
            for (var i = 0; i < masterStreams.Count; i++)
            {
                var entry = masterStreams[i];
                //Check if we reached our goal entry
                if (entry.frameID > frameID)
                    return;
                //Frame is already unpacked
                if (entry.basedOnID == 0)
                    continue;
                //Find baseFrame
                bool found = TryGet(entry.basedOnID, out var baseStream, out int basedOnID);
                if (!found)
                    throw new Exception($"Cannot find baseFrame of {entry.frameID}");

                var unpackedStream = unpackFunc.Invoke(baseStream, entry.stream);
                Insert(new Entry()
                {
                    basedOnID = 0,
                    frameID = entry.frameID, 
                    stream = unpackedStream
                });
            }
        }
    }
}