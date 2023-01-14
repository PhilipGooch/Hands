using NBG.Core.Streams;
using System;

namespace NBG.Net
{
    public static class NetStreamExtensions
    {
        internal const int NetIDSmallBits = 8;
        internal const int NetIDLargeBits = 16;
        public static uint ReadID(this IStreamReader reader)
        {
            uint ret = reader.ReadUInt32(NetIDSmallBits, NetIDLargeBits);
            return ret;
        }

        public static void WriteID(this IStreamWriter reader, uint id)
        {
            reader.Write(id, NetIDSmallBits, NetIDLargeBits);
        }

        internal const int NetStreamSmallBits = 8;
        internal const int NetStreamLargeBits = 16;
        public static void WriteStream(this IStreamWriter writer, IStream netStream)
        {
            if (netStream == null)
            {
                writer.Write(0, NetStreamSmallBits, NetStreamLargeBits);
            }
            else
            {
                var len = netStream.LimitBits;
                if (len < 0)
                    throw new Exception("Stream has no read limit set");
                len -= netStream.PositionBits;
                if (len < 0)
                    return;
                var fullBytes = len / 8;
                var remainingBits = (ushort)(len % 8);
                if (len > 1 << NetStreamLargeBits)
                    throw new Exception($"WriteStream error: NetStream size {netStream.BitsAvailable()} exceeds NetStreamLargeBits limit {1<<NetStreamLargeBits}");
                writer.Write(len, NetStreamSmallBits, NetStreamLargeBits);
                for (int i = 0; i < fullBytes; i++)
                    writer.Write(netStream.ReadByte());
                if (remainingBits > 0)
                    writer.Write(netStream.ReadUInt32(remainingBits), remainingBits);
            }
        }

        public static void CopyStreamData(this IStreamWriter writer, IStream netStream)
        {
            if (netStream == null)
            {
                return;
            }
            
            var len = netStream.LimitBits;
            if (len <= 0)
                throw new Exception("Stream has no read limit set");
            len -= netStream.PositionBits;
            
            if (len <= 0)
                return;
            var fullBytes = len / 8;
            var remainingBits = (ushort)(len % 8);
            if (len > 1 << NetStreamLargeBits)
                throw new Exception($"WriteStream error: NetStream size {netStream.BitsAvailable()} exceeds NetStreamLargeBits limit {1<<NetStreamLargeBits}");
            for (int i = 0; i < fullBytes; i++)
                writer.Write(netStream.ReadByte());
            if (remainingBits > 0)
                writer.Write(netStream.ReadUInt32(remainingBits), remainingBits);
        }
        public static IStream ReadStream(this IStreamReader reader)
        {
            if (reader.LimitBits == 0)
                throw new NotSupportedException("Empty stream is not supported");

            var lenBits = reader.ReadUInt32(NetStreamSmallBits, NetStreamLargeBits);
            var lenBytes = (int)(lenBits + 7) / 8; 
            var ret = BasicStream.Allocate(lenBytes, (int)lenBits);
            
            //Read all full bytes
            for (int i = 0; i < lenBits / 8; i++)
                ret.Write(reader.ReadByte());
            
            //read remaining bits
            var remainingBits = (ushort)(lenBits % 8);
            if (remainingBits > 0)
                ret.Write(reader.ReadUInt32(remainingBits), remainingBits);
            //Reset to start
            ret.Seek(0);
            return ret;
        }
        
        public static void WriteMsgId(this IStreamWriter writer, ushort msgID)
        {
            writer.Write((uint) msgID, 16);
        }
        public static ushort ReadMsgId(this IStreamReader reader)
        {
            return (ushort)reader.ReadUInt32(16);
        }
    }
}