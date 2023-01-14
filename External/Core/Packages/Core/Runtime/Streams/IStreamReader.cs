namespace NBG.Core.Streams
{
    public interface IStreamReader
    {
        bool ReadBool();
        byte ReadByte();
        int ReadInt32(ushort bitfull);
        int ReadInt32(ushort bitsmall, ushort bitlarge);
        int ReadInt32(ushort bitsmall, ushort bitmed, ushort bitlarge);
        uint ReadUInt32(ushort bitfull);
        uint ReadUInt32(ushort bitsmall, ushort bitlarge);
        uint ReadUInt32(ushort bitsmall, ushort bitmed, ushort bitlarge);
        byte[] ReadArray(ushort lenBytes);
        int LimitBytes { get; }
        int LimitBits { get; }
        bool BitsAvailable(uint count = 1);
    }
}
