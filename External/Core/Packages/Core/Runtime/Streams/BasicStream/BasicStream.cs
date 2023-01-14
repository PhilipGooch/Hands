using UnityEngine;

namespace NBG.Core.Streams
{
    public static class BasicStream
    {
        public static IStream Allocate(int capacity, int limitBits = 0)
        {
            return new BasicStreamImpl(new byte[capacity], 0, limitBits);
        }
        public static IStream AllocateFromStream(IStream baseStream, int offsetBits = 0)
        {
            Debug.Assert(baseStream != null, "baseStream cannot be null");
            var ret = new BasicStreamImpl(baseStream.Buffer, offsetBits, baseStream.LimitBits);
            return ret;
        }
        public static IStream AllocateFromBuffer(byte[] buffer, int offsetBits = 0, int limitBits = 0)
        {
            var ret = new BasicStreamImpl(buffer, offsetBits, limitBits);
            return ret;
        }
    }
}
