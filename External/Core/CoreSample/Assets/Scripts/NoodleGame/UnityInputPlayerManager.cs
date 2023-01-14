using UnityEngine;
using UnityEngine.InputSystem;

public abstract class UnityInputPlayerManager<TPlayerController, TControls> : PlayerManagerBase<TPlayerController, TControls, InputDevice>
    where TPlayerController : PlayerControllerBase
    where TControls : IPlayerControls<InputDevice>
{
    [SerializeField] internal InputActionProperty m_JoinAction;
    public override void OnCreate()
    {
        base.OnCreate();
        InputSystem.onDeviceChange += InputSystem_onDeviceChange;
        m_JoinAction.action.performed += OnJoinPressed;
        m_JoinAction.action.Enable();
    }
    public override void Dispose()
    {
        m_JoinAction.action.Disable();
        m_JoinAction.action.performed -= OnJoinPressed;
        InputSystem.onDeviceChange -= InputSystem_onDeviceChange;
        base.Dispose();
    }
    private void InputSystem_onDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
                OnDeviceAdded(device);
                break;
            case InputDeviceChange.Disconnected:
                OnDeviceRemoved(device);
                break;
        }
    }
    public void OnJoinPressed(InputAction.CallbackContext context)
    {
        var device = context.control.device;
        OnJoinPressed(device);
    }
    public static bool reenableJoinActionFix = true;
    public void Update()
    {
        EnsureJoinActionEnabled();
    }
    public void EnsureJoinActionEnabled()
    {
        // error - after changing the device list, action needs reenabling
        if (reenableJoinActionFix)
        {
            m_JoinAction.action.Disable();
            m_JoinAction.action.Enable();
            reenableJoinActionFix = false;
        }
    }
}

