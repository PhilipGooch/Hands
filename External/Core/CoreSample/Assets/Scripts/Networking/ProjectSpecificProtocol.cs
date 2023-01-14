using NBG.Net;

namespace CoreSample.Network
{
    [Protocol]
    public class ProjectSpecificProtocol
    {
        public static ushort LevelLoad; // Server->client
        public static ushort LevelLoadAck; // Client->server
    }
}
