namespace NBG.Net.PlayerManagement
{
    [Protocol]
    public static class PlayerManagementProtocol
    {
        //Player input
        public static ushort PlayerInput; //Client -> server
        public static ushort PlayerInputAck; //Server -> client
        //Player Management
        public static ushort PlayerAdded; //Server -> client(broadcast), when new player spawns
        public static ushort PlayerRemoved; //Server -> client(broadcast), when player despawns
        public static ushort RequestPlayer; //Client -> server, after connect or when adding a new split screen player. Async, requires results
        public static ushort RemovePlayer; //Client -> server, whenever you want to remove a split screen player.
        public static ushort RequestPlayerSuccess; //Server -> client, AFTER PlayerAdded message, contains mapping
        public static ushort RequestPlayerFailed; //Server -> client, when server full, etc.
    }
}