namespace NBG.Net
{
    public enum ConnectionClosedReason : byte
    {
        Undefined,
        Connection, //The underlying connection was terminated
        ServerShutdown, //The Server is shutting down, only send from Host to Clients
        ClientShutdown, //The Client is shutting down, only send from Clients to Hosts
        ServerFull, //This Server already has maximum Connections/Players
        Kick, //Kicked from the Server and not welcome back until server has been restarted
        Refused, //other side turned us away
        Timeout, //other side did not answer in time
    }
}