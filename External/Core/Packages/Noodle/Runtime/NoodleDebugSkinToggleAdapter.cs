namespace Noodles
{
    public interface INoodleDebugSkinToggle
    {
        void Toggle();
        bool showSkin { get; }
        bool showDebug { get; }
    }

    public static class NoodleDebugSkinToggleAdapter
    {
        public static INoodleDebugSkinToggle adapter;
    }
}
