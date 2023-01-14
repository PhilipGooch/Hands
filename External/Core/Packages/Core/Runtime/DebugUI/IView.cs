using System.Collections.Generic;

namespace NBG.DebugUI
{
    public interface IView
    {
        void Show();
        void Hide();
        void UpdateExtraInfo(string text);
        void UpdateView(IDebugTree tree);
        void UpdateSelection(string category, int itemIndexInCategory);
        void PushLog(string message, Verbosity verbosity);
    }
}
