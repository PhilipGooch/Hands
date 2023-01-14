namespace Recoil
{
    /// <summary>
    /// Implement to take over control of Recoil body registration
    /// </summary>
    public interface IHandlesRigidbodies
    {
        void OnRegisterRigidbodies();
        void OnUnregisterRigidbodies();
    }
}
