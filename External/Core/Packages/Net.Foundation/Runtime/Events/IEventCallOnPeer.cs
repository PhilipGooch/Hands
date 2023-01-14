namespace NBG.Net
{
    public interface IEventCallOnPeer
    {
        void CallOnPeer<T>(T eventData, IPeer peer) where T : struct;
    }
}
