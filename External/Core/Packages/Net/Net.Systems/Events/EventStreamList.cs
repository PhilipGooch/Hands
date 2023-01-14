using System;
using System.Collections.Generic;
using System.Linq;
using NBG.Core.Streams;
using UnityEngine;

namespace NBG.Net.Systems
{
    /// <summary>
    /// This handles all of Event Streams for sending, resending, receiving and processing.
    /// </summary>
    internal class EventStreamList
    {
        private readonly List<Entry> masterStreams = new List<Entry>();
        private readonly CompareByID compareByID = new CompareByID();
        private readonly Dictionary<INetTransportPeer, int> lastAcks = new Dictionary<INetTransportPeer, int>();
        private readonly Dictionary<INetTransportPeer, IStream> perPeerStreams = new Dictionary<INetTransportPeer, IStream>();
        private readonly int overflowLimit; //This catches overflows by malicious clients. See @overflowLimit in constructor
        private int currentHistoryLimit;

        /// <summary>
        /// Makes a new Event List.
        /// </summary>
        /// <param name="overflowLimit">Worst case overflow to harden this against malicious clients</param>
        internal EventStreamList(int overflowLimit = 1024)
        {
            this.overflowLimit = overflowLimit;
        }
        /// <summary>
        /// Inserts a new Event stream, as received from server.
        /// NOTE: Does NOT support overwriting (inserting a different stream for the same serverFrameID) existing
        /// entries, they will be silently ignored.
        /// </summary>
        /// <param name="eventStream">A stream of event data</param>
        /// <param name="serverFrameID">The server frame this event was created. This can be in the past, present or future.</param>
        internal void Insert(IStream eventStream, int serverFrameID)
        {
            if (serverFrameID < currentHistoryLimit)
            {
                /*Note: This event was from a package we have not acked. But we have acked a package more recent,
                 which had included this event (barring Bugs). But because its so old, we have no memory of it or its execution.
                 We reject this event, to prevent double execution.
                */
                Debug.LogWarning($"{serverFrameID} is older than current event history limit {currentHistoryLimit}");
                return;
            }
            var newEntry = new Entry()
            {
                frameID = serverFrameID,
                stream = eventStream,
            };
            var pos = masterStreams.BinarySearch(newEntry, compareByID);
            if (pos < 0)
            {
                //New event frame -> Insert at best position for this
                masterStreams.Insert(~pos, newEntry);
            }
            //NOTE: No else case, but this happens when acks are dropped. Server will resend, until an ack was send through, but we will ignore it.
        }
        internal IStream GetPeerStream(INetTransportPeer peer)
        {
            if (!perPeerStreams.TryGetValue(peer, out var stream))
            {
                stream = BasicStream.Allocate(1024);
                perPeerStreams[peer] = stream;
            }
            return stream;
        }
        /// <summary>
        /// Throws away all event streams that have been seen by all peers
        /// </summary>
        internal void LimitHistoryByAcks()
        {
            if (lastAcks.Count > 0)
            {
                int lowWaterMark = lastAcks.Values.Min(x => x);
                if (lowWaterMark > 0)
                {
                    LimitHistory(lowWaterMark);
                }
            }
            else
            {
                masterStreams.Clear();
            }
        }
        /// <summary>
        /// Collects all event streams, based on acks, that are interesting for this peer
        /// </summary>
        /// <param name="processor">The delegate that will be called for each event stream</param>
        /// <param name="peer">The peer to process events for</param>
        /// <param name="currentFrameID">The frameID that is currently build and this processor will attach to</param>
        internal void ProcessForPeer(Action<int, IStream> processor, INetTransportPeer peer, int currentFrameID)
        {
            //Find start stream
            var newEntry = new Entry()
            {
                frameID = GetLastAck(peer),
                stream = null,
            };
            //Write master streams, depending on what that Peer acked
            var masterStreamIndex = masterStreams.BinarySearch(newEntry, compareByID);
            if (masterStreamIndex < 0)
                masterStreamIndex = ~masterStreamIndex + 1;
            while (masterStreamIndex < masterStreams.Count)
            {
                var startEntry = masterStreams[masterStreamIndex];
                processor.Invoke(startEntry.frameID, startEntry.stream);
                startEntry.stream.Seek(0);
                masterStreamIndex++;
            }
            //send per-Peer streams immediately
            if (perPeerStreams.TryGetValue(peer, out var stream) && stream.PositionBits > 0)
            {
                stream.Flip();
                processor.Invoke(currentFrameID, stream);
                stream.Reset();
            }
        }
        /// <summary>
        /// Used to free memory by throwing away streams beyond a certain age
        /// This also honors the overflow limit.
        /// </summary>
        /// <param name="oldestFrameID"></param>
        internal void LimitHistory(int oldestFrameID)
        {
            currentHistoryLimit = oldestFrameID -1;
            var entry = new Entry()
            {
                frameID = oldestFrameID,
                stream = null,
            };
            var pos = masterStreams.BinarySearch(entry, compareByID);
            if (pos >= 0)
            {
                //Drop all until lowWaterMark, but keep lowWaterMark frame for interpolation
                var toRemove = Math.Max(pos, masterStreams.Count - overflowLimit) - 1;
                if (toRemove > 500)
                {
                    //Likely due to lag. If you keep seeing those, there are connection issues
                    Debug.LogWarning($"Dropping {toRemove} Frames at once.");
                }

                if (toRemove > 0)
                {
                    masterStreams.RemoveRange(0, toRemove);
                    //Debug.Log($"Limit history dropped streams up until frame: {toRemove} with {masterStreams.Count} remaining");
                }
            }
            else
            {
                //low water mark was not exactly found. But we can remove everything up until where it would be
                var removeCount = (~pos)-1;
                if (removeCount > 0)
                {
                    masterStreams.RemoveRange(0, removeCount);    
                }
            }
        }
        /// <summary>
        /// Client side processing of events.
        /// </summary>
        /// <param name="action">Delegate that will be called for any event that has not been replayed</param>
        /// <param name="limitFrameID">Upper limit of how far events should be played</param>
        public void ProcessEvents(Action<int, IStream> action, int limitFrameID)
        {
            for (var i = 0; i < masterStreams.Count; i++)
            {
                var currentEntry = masterStreams[i];
                //Only process events up until limit
                if (currentEntry.frameID > limitFrameID)
                    break;
                //Don't process frames that have been processed already
                if (currentEntry.stream == null)
                    continue; 
                action.Invoke(currentEntry.frameID, currentEntry.stream);
                //Mark event as processed
                currentEntry.stream = null;
            }
        }
        /// <summary>
        /// Query acked frame IDs for peers
        /// </summary>
        /// <param name="peer">The peer you want the ack for</param>
        /// <returns>Last FrameID that was successfully acked by the peer</returns>
        private int GetLastAck(INetTransportPeer peer)
        {
            if (!lastAcks.TryGetValue(peer, out var ret))
            {
                return -1;
            }

            return ret;
        }
        /// <summary>
        /// Set last ack for a peer.
        /// </summary>
        /// <param name="peer">The peer that has send this ack</param>
        /// <param name="frameID">The frameID that was acked</param>
        internal void UpdateLastAck(INetTransportPeer peer, int frameID)
        {
            lastAcks[peer] = frameID;
        }
        
        internal void RemovePeer(INetTransportPeer peer)
        {
            lastAcks.Remove(peer);
            perPeerStreams.Remove(peer);
        }
        
        /// <summary>
        /// Helper struct.
        /// frameID is the frameID of that stream
        /// stream is the raw event stream data. Will be null after it has been replayed.
        /// Will be collected after we know that the Server has received our ack
        /// </summary>
        private struct Entry
        {
            public int frameID;
            public IStream stream;
        }
        /// <summary>
        /// Utility comparator. Many functions in this class use binary search on FrameIDs.
        /// </summary>
        private class CompareByID : IComparer<Entry>
        {
            public int Compare(Entry x, Entry y)
            {
                return x.frameID.CompareTo(y.frameID);
            }
        }
    }
}