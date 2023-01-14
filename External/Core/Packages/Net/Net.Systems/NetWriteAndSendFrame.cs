using NBG.Core.GameSystems;
using NBG.Core.Streams;
using NBG.DebugUI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = NBG.Core.Logger;

namespace NBG.Net.Systems
{
    public interface INetFrameCollector
    {
        IStream Collect();
        void Validate(IStreamReader stream);
        IStream CalculateDelta(IStreamReader fullStream, IStreamReader lastAckedStream);
    }

    //TODO@!: consider changing the frame to contain root data (frame id), and multiple inner streams which can then handle themselves via a common StreamList interface
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public class NetWriteAndSendFrame : GameSystem
    {
        private static Logger logger = new Logger(nameof(NetWriteAndSendFrame));
        public const int REMOTE_STATE_LIMIT = 60;
        public const int FULL_STATE_INITIAL_SIZE = 16 * 1024; //16 kb
        private NetEventBus netEventBus;

        private int currentFrameID = 0;
        private MasterStreamList masterStreams;
        public IPeerCollection allPeers;

        public int CurrentFrameID => currentFrameID;
        private IDebugItem frameSizeDebugItem;
        private IDebugItem deltaSizeItem;
        private IDebugItem masterStreamListLength;

        private List<INetFrameCollector> collectors = new List<INetFrameCollector>();

        private int deltaTotalBits;

        protected override void OnStartRunning()
        {
            netEventBus = World.GetExistingSystem<NetEventBus>();
            Debug.Assert(netEventBus != null);
            
            frameSizeDebugItem = DebugUI.DebugUI.Get().RegisterObject("Last Frame Size", "Network", () =>
            {
                var newestFrame = masterStreams.GetNewestFrameID();
                if (masterStreams.TryGet(newestFrame, out var stream, out _))
                {
                    return $"Frame {newestFrame} is {stream.LimitBits} bits ({stream.LimitBytes} bytes)";    
                }
                else
                {
                    return "Networking not yet started";
                }
            });
            deltaSizeItem = DebugUI.DebugUI.Get().RegisterObject("DeltaSizes", "Network", () =>
            {
                var newestFrame = masterStreams.GetNewestFrameID();
                if (masterStreams.TryGet(newestFrame, out var stream, out _))
                {
                    var NumPeers = allPeers.GetReadyPeers().Count();
                    var deltaTotalBytes = (deltaTotalBits +7 ) /8;
                    return $"{NumPeers} peers, delta total {deltaTotalBits} bits ({deltaTotalBytes} bytes))" ;    
                }
                else
                {
                    return "Networking not yet started";
                }
            });
            masterStreamListLength = DebugUI.DebugUI.Get().RegisterObject("MasterStreams", "Network", () 
                => $"{masterStreams.Count} streams using {masterStreams.Mem} bytes");
        }

        protected override void OnStopRunning()
        {
            netEventBus = null;
            DebugUI.DebugUI.Get().Unregister(frameSizeDebugItem);
            DebugUI.DebugUI.Get().Unregister(deltaSizeItem);
            DebugUI.DebugUI.Get().Unregister(masterStreamListLength);
        }

        protected override void OnCreate()
        {
            this.masterStreams = new MasterStreamList(REMOTE_STATE_LIMIT);

            collectors.Add(NetBehaviourList.instance); //TODO@! - eliminate

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            currentFrameID++;
            masterStreams.LimitHistoryByAcks();
            var lastCollectedStream = Collect(currentFrameID);
            SendFrame(lastCollectedStream);
            //Validate(currentFrameID);
        }

        //NOTE: This is not production code and will later be (re)moved. Used to validate precision values while developing systems. ~~~Sebastian
        private void Validate(int serverFrameID)
        {
            _ = masterStreams.TryGet(serverFrameID, out IStream stream, out _);
            stream.Seek(0);
            var frameID = stream.ReadFrameId();
            Debug.Assert(frameID == serverFrameID);
            for (int i = 0; i < collectors.Count; ++i)
            {
                var collector = collectors[i];
                var internalStream = stream.ReadStream();
                collector.Validate(internalStream);
            }
        }

        private IStream Collect(int serverFrameID)
        {
            IStream full = BasicStream.Allocate(FULL_STATE_INITIAL_SIZE);
            for (int i = 0; i < collectors.Count; ++i)
            {
                var collector = collectors[i];
                var stream = collector.Collect();
                full.WriteStream(stream);
            }
            full.Flip();
            masterStreams.Insert(full, serverFrameID);
            return full;
        }

        // Frame can have multiple messages:
        // [2 bytes] header [n bytes] data
        private void SendFrame(IStream fullStream)
        {
            deltaTotalBits = 0;
            foreach (var peer in allPeers.GetReadyPeers())
            {
                fullStream.Seek(0);

                IStream lastAckedStream = null;
                IStreamWriter writer = peer.BeginSend(ChannelType.Unreliable);

                var lastAck = masterStreams.GetLastAck(peer);
                var wasAcked = (lastAck > 0 && masterStreams.TryGet(lastAck, out lastAckedStream, out _));
                if (wasAcked) //Was acked and found -> calculate send delta 
                {
                    writer.WriteMsgId(NetBehaviourListProtocol.DeltaFrame);
                    writer.WriteFrameId(currentFrameID);
                    writer.WriteFrameId(lastAck);
                    lastAckedStream.Seek(0);
                    Debug.Assert(lastAckedStream != null);

                    IStream collectorDeltas = BasicStream.Allocate(1024);
                    for (int i = 0; i < collectors.Count; ++i)
                    {
                        var lastAckedData = lastAckedStream.ReadStream();
                        var fullStreamData = fullStream.ReadStream();
                        var collector = collectors[i];
                        var collectorDelta = collector.CalculateDelta(fullStreamData, lastAckedData);
                        collectorDeltas.WriteStream(collectorDelta);
                    }
                    collectorDeltas.Flip();
                    writer.WriteStream(collectorDeltas);
                }
                else //Was not acked, send full stream
                {
                    writer.WriteMsgId(NetBehaviourListProtocol.MasterFrame);
                    writer.WriteFrameId(currentFrameID);
                    writer.WriteFrameId(0);
                    writer.WriteStream(fullStream);
                }

                writer.WriteMsgId(NetEventBusProtocol.Events);
                writer.WriteFrameId(currentFrameID);
                netEventBus.AppendToFrame(peer, writer, currentFrameID);

                deltaTotalBits += (writer as IStream).PositionBits; //TODO: Hacky cast.
                peer.EndSend();
            }
        }

        public void LimitHistoryToCurrentFrame()
        {
            masterStreams.ClearHistory();
        }

        public void DeltaAck(INetTransportPeer context, IStreamReader data)
        {
            var ackedFrame = data.ReadFrameId();
            //Check if potentially malicious client tries to ack a packet in the future.
            if (ackedFrame > currentFrameID)
            {
                Debug.LogWarning($"Client {context} tried to ack frame {ackedFrame} but most recent is {currentFrameID}.");
                return; //Do not process garbage
            }
            masterStreams.UpdateLastAck(context, ackedFrame);
            netEventBus.UpdateLastAck(context, ackedFrame);
        }

        public void PeerDisconnected(INetTransportPeer context)
        {
            masterStreams.RemovePeer(context);
        }
    }
}
