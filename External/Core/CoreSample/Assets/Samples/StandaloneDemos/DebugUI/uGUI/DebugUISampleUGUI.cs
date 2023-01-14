using NBG.DebugUI;
using NBG.DebugUI.View.uGUI;

public class DebugUISampleUGUI : DebugUISampleBase
{
    protected override IView CreateView()
    {
        return UGUIView.GetScreenSpace();
    }
}
