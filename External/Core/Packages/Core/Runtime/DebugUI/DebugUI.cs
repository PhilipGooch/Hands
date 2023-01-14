using System;
using NBG.Core;

namespace NBG.DebugUI
{
    public enum Verbosity
    {
        Info,
        Warning,
        Error
    }

    public interface IDebugUI
    {
        bool IsVisible { get; }

        // Setup the view to use
        void SetView(IView view);

        // Shows the UI
        void Show();
        event Action OnShow;

        // Hides the UI
        void Hide();

        // Updates the UI given a new <inputState>
        void Update(Input inputState);

        // Sets custom text on the title bar
        void SetExtraInfoText(string text);

        // Output a message inside the debug view
        void Print(string message, Verbosity verbosity = Verbosity.Info);
        event Action<string, Verbosity> OnPrint;

        // Removes a previously registered item
        void Unregister(IDebugItem id);


        // Adds an non-interactive item with a boolean state display
        IDebugItem RegisterBool(string label, string category, Func<bool> getValue);

        // Adds an non-interactive item with an integer state display
        IDebugItem RegisterInt(string label, string category, Func<int> getValue);

        // Adds an non-interactive item with a float state display
        IDebugItem RegisterFloat(string label, string category, Func<double> getValue);
        
        // Adds an non-interactive item which uses ToString() to display
        IDebugItem RegisterObject(string label, string category, Func<object> getValue);


        // Adds an interactive item
        // Activating calls <onPress>
        IDebugItem RegisterAction(string label, string category, Action onPress);


        // Adds an interactive item with a boolean state display
        // Activating or switching calls <setValue> with inverse <getValue> return value
        IDebugItem RegisterBool(string label, string category, Func<bool> getValue, Action<bool> setValue);

        // Adds an interactive item with an integer state display
        // Switching calls <setValue> with <getValue> return value incremented/decremented by <step> based on direction
        IDebugItem RegisterInt(string label, string category, Func<int> getValue, Action<int> setValue, int step = 1, int minValue = int.MinValue, int maxValue = int.MaxValue);

        // Adds an interactive item with a float state display
        // Switching calls <setValue> with <getValue> return value incremented/decremented by <step> based on direction
        IDebugItem RegisterFloat(string label, string category, Func<double> getValue, Action<double> setValue, double step = 0.001, double minValue = float.MinValue, double maxValue = float.MaxValue); // Clamp to float range by default

        // Adds an interactive item with an enum state display
        // Switching calls <setValue> with <getValue> incremented/decremented return value
        IDebugItem RegisterEnum(string label, string category, Type enumType, Func<Enum> getValue, Action<Enum> setValue);
    }

    public static class DebugUI
    {
        [ClearOnReload]
        static DebugUIImpl _instance;

        public static IDebugUI Get()
        {
            if (_instance == null)
                _instance = new DebugUIImpl();
            return _instance;
        }

        public static bool IsCreated => (_instance != null);
    }
}
