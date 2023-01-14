namespace NBG.Core.Streams
{
    public interface IStreamWriter
    {
        void Write(bool v);
        void Write(byte b);
        void Write(int x, ushort bitsmall, ushort bitlarge);
        void Write(int x, ushort bitsmall, ushort bitmed, ushort bitlarge);
        void Write(int x, ushort bits);
        void Write(uint x, ushort bitfull);
        void Write(uint x, ushort bitsmall, ushort bitlarge);
        void Write(uint x, ushort bitsmall, ushort bitmed, ushort bitlarge);
        void WriteArray(byte[] array, ushort lenBytes);
    }
}
