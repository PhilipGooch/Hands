namespace NBG.Net
{
    /// <summary>
    /// Network authority changed event.
    /// </summary>
    public struct NetworkAuthorityChangedEvent
    {
        public readonly NetworkAuthority networkAuthority;

        public NetworkAuthorityChangedEvent(NetworkAuthority networkAuthority)
        {
            this.networkAuthority = networkAuthority;
        }
    }
}
