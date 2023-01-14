using UnityEngine;
using VR.System;

#if CHEATS_ENABLED
using NBG.DebugUI;
using Input = UnityEngine.Input;
using NBG.DebugUI.View.uGUI;
#endif

public class DebugUI : MonoBehaviour
{

    public Transform viewTarget;
    public Camera cam;

#if CHEATS_ENABLED

    Player player;
    IDebugUI _debugUI;

    const float kActivationThresh = 0.5f;

    protected IView CreateView()
    {
        return UGUIView.GetWorldSpace(viewTarget, cam, 7);
    }

    static LogType LogTypeFromVerbosity(Verbosity verbosity)
    {
        switch (verbosity)
        {
            case Verbosity.Info: return LogType.Log;
            case Verbosity.Warning: return LogType.Warning;
            case Verbosity.Error: return LogType.Error;
            default:
                throw new System.NotImplementedException();
        }
    }

    void Awake()
    {
        player = Player.Instance;

        _debugUI = NBG.DebugUI.DebugUI.Get();
        _debugUI.SetView(CreateView());

        _debugUI.OnPrint += (message, verbosity) =>
        {
            Debug.LogFormat(LogTypeFromVerbosity(verbosity), LogOption.NoStacktrace, null, message);
        };

        //_debugUI.Print(textToPrint, LogType);

        _debugUI.SetExtraInfoText("The sheep goes baa");
    }


    //LEFT HAND:
    // A - open debug UI
    // Joystick - change categories
    // 
    // RIGHT HAND: 
    // A - Ok
    // Joystick - up/down , increment/decrement
    //

    void Update()
    {
        if (player.leftHand.GetInput(HandInputType.aButtonDown))
        {
            if (!_debugUI.IsVisible)
            {
                _debugUI.Show();
                player.SetControllerMovementEnabled(true);
                //UICamera.Instance.AddActiveUIElement();
            }
            else
            {
                _debugUI.Hide();
                player.SetControllerMovementEnabled(false);
                //UICamera.Instance.RemoveActiveUIElement();

            }
        }

        if (!_debugUI.IsVisible)
            return;

        var leftInput = player.leftHand.MoveDir;
        var rightInput = player.rightHand.MoveDir;

        bool right = Input.GetKeyDown(KeyCode.RightArrow);
        bool left = Input.GetKeyDown(KeyCode.LeftArrow);

        bool up = Input.GetKeyDown(KeyCode.UpArrow);
        bool down = Input.GetKeyDown(KeyCode.DownArrow);

        bool categoryLeft = Input.GetKeyDown(KeyCode.Q);
        bool categoryRight = Input.GetKeyDown(KeyCode.E);

        bool ok = player.rightHand.GetInput(HandInputType.aButtonDown);


        if (Mathf.Abs(rightInput.y) >= kActivationThresh)
        {
            if (Mathf.Sign(rightInput.y) > 0)
                up = true;
            else
                down = true;
        }

        if (Mathf.Abs(leftInput.x) >= kActivationThresh)
        {
            if (Mathf.Sign(leftInput.x) > 0)
                categoryRight = true;
            else
                categoryLeft = true;
        }

        if (Mathf.Abs(rightInput.x) >= kActivationThresh)
        {
            if (Mathf.Sign(rightInput.x) > 0)
                right = true;
            else
                left = true;
        }

        var input = new NBG.DebugUI.Input();
        input.up = up;
        input.down = down;
        input.left = left;
        input.right = right;
        input.categoryLeft = categoryLeft;
        input.categoryRight = categoryRight;
        input.ok = ok;

        _debugUI.Update(input);
    }
#endif

}
