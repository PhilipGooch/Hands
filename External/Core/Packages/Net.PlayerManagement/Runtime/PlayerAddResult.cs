namespace NBG.Net.PlayerManagement
{
    public enum PlayerAddResult : byte
    {
        InternalError,
        Success,
        FailedLocalLimit,
        FailedServerLimit,
    }
}
