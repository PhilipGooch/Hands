namespace NBG.Net
{
    /// <summary>
    /// Peer join-in-progress event.
    /// </summary>
    public struct NewPeerIsReadyEvent
    {
        IEventCallOnPeer target;
        IPeer peer;

        public NewPeerIsReadyEvent(IEventCallOnPeer target, IPeer peer)
        {
            this.target = target;
            this.peer = peer;
        }

        public void CallOnPeer<T>(T eventData) where T : struct
        {
            target.CallOnPeer(eventData, peer);
        }
    }
}
