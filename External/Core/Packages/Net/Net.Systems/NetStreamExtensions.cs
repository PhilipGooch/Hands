using System.Collections.Generic;
using NBG.Core.Streams;

namespace NBG.Net.Systems
{
    public static class NetStreamExtensions
    {      
        /*public static void WritePos(this IStreamWriter writer, float3 pos, float posRange, ushort bits)
        {
            var posQuant = pos.Quantize(posRange, bits);
            writer.Write(posQuant.x, bits);
            writer.Write(posQuant.y, bits);
            writer.Write(posQuant.z, bits);
        }
        
        public static float3 ReadPos(this IStreamReader reader, float posRange, ushort bits)
        {
            var value = new int3
            {
                x = reader.ReadInt32(bits),
                y = reader.ReadInt32(bits),
                z = reader.ReadInt32(bits),
            };
            return value.Dequantize(posRange, bits);
        }*/
       
        public static int ReadFrameId(this IStreamReader reader)
        {
            return reader.ReadInt32(8, 32);
        }

        public static void WriteFrameId(this IStreamWriter writer, int frameId)
        {
            writer.Write(frameId, 8, 32);
        }
        internal static Dictionary<uint, IStream> ReadScopes(this IStreamReader stream)
        {
            var ret = new Dictionary<uint, IStream>();
            var current = stream.ReadStream();
            while (current != null)
            {
                var id = current.ReadID();
                ret[id] = current;
                if (!stream.BitsAvailable())
                    break;
                current = stream.ReadStream();
            }
            return ret;
        }
    }
}