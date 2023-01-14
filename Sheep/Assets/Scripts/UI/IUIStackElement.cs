public interface IUIStackElement
{
    public bool Blocker { get; }
    public bool DoFadeOut { get; }

    void OnBecomeActive();
    void OnBecomeInactive();
}
