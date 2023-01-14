namespace NBG.DebugUI
{
    public interface IDebugItem
    {
        string Label { get; }

        // Set priority order in the item list
        // Higher <priority> values are shown first
        int Priority { get; set; }

        // Disabling an item makes it non-interactable
        bool Enabled { get; set; }


        // Can it be activated?
        bool HasActivation { get; }
        // Can the value be changed?
        bool HasSwitching { get; }
        // Value as shown in the UI
        string DisplayValue { get; }
    }
}
