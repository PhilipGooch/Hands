using UnityEngine;
using NBG.VehicleSystem;
using NBG.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Class mostly for testing vehicles. Can be used both with new and old unity input system
/// </summary>
public class ReferenceVehicleInputHandler: MonoBehaviour, IManagedBehaviour
{
    float acceleration;
    float braking;
    float steering;
    float oldSteering;
    int currentGear;

    IChassis chassis;
    IEngine engine;

    [SerializeField] bool manualGears = false;
    [SerializeField]
    private float steeringSpeed = 1;

    [SerializeField]
    private PIDController acceleratorPID = new PIDController();

    float verticalAxis = 0;
    float horizontalAxis = 0;
    bool spaceIsPressed = false;
    bool qIsPressed = false;
    bool increaseGear = false;
    bool decreaseGear = false;

    float AccelerationValue { get => acceleration; }
    float SteeringInputValue { get => steering; }
    public int CurrentGear
    {
        get => currentGear;
    }

    float BrakesInputValue { get => braking; }
    public bool QIsPressedThisFrame { get => qIsPressed; }

    void OnValidate()
    {
        acceleratorPID.Clamp = true;
        acceleratorPID.ClampMin = 0.0f;
        acceleratorPID.ClampMax = 1.0f;
    }

    void IManagedBehaviour.OnLevelLoaded()
    {
        chassis = GetComponent<IChassis>();
        if (chassis == null)
            Debug.LogWarning($"{nameof(ReferenceVehicleInputHandler)} expects an {nameof(IChassis)} component.");
        
        engine = GetComponent<IEngine>();
        if (engine == null)
            Debug.LogWarning($"{nameof(ReferenceVehicleInputHandler)} expects an {nameof(IEngine)} component.");
    }

    void IManagedBehaviour.OnAfterLevelLoaded()
    {
    }

    void IManagedBehaviour.OnLevelUnloaded()
    {
        
    }

    void Update()
    {
        steering = 0;
        verticalAxis = 0;
        horizontalAxis = 0;
        spaceIsPressed = false;
        qIsPressed = false;
        increaseGear = false;
        decreaseGear = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.IsActuated())
        {
            if (Keyboard.current.aKey.isPressed)
            {
                horizontalAxis = -1;
            }
            if (Keyboard.current.dKey.isPressed)
            {
                horizontalAxis = 1;
            }
            if (Keyboard.current.wKey.isPressed)
            {
                verticalAxis = 1;
            }
            if (Keyboard.current.sKey.isPressed)
            {
                verticalAxis = -1;
            }
            spaceIsPressed = Keyboard.current.spaceKey.isPressed;
            qIsPressed = Keyboard.current.qKey.isPressed;
            increaseGear = Keyboard.current.rKey.wasPressedThisFrame;
            decreaseGear = Keyboard.current.fKey.wasPressedThisFrame;
        }
        else if (Gamepad.current != null && Gamepad.current.IsActuated())
        {
            var vectorValue = Gamepad.current.leftStick.ReadUnprocessedValue();
            if (vectorValue != Vector2.zero)
            {
                horizontalAxis = vectorValue.x;
                verticalAxis = vectorValue.y;
            }
            spaceIsPressed = Gamepad.current.aButton.isPressed;
            qIsPressed = Gamepad.current.yButton.isPressed;
            increaseGear = Gamepad.current.rightShoulder.wasPressedThisFrame;
            decreaseGear = Gamepad.current.leftShoulder.wasPressedThisFrame;
        }
#else
        verticalAxis = Input.GetAxis("Vertical");
        horizontalAxis = Input.GetAxis("Horizontal");
        spaceIsPressed = Input.GetKey(KeyCode.Space);
        qIsPressed = Input.GetKey(KeyCode.Q);
        increaseGear = Input.GetKeyDown(KeyCode.R);
        decreaseGear = Input.GetKeyDown(KeyCode.F);
#endif
    }

    void FixedUpdate()
    {
        steering = Mathf.MoveTowards(oldSteering, horizontalAxis, steeringSpeed * Time.fixedDeltaTime);
        oldSteering = steering;

        if (manualGears)
        {
            if (increaseGear)
            {
                currentGear++;
            }
            if (decreaseGear)
            {
                currentGear--;
            }
        }
        else
        {
            currentGear = 0;
            if (verticalAxis > 0)
            {
                currentGear = 1;
            }
            else if (verticalAxis < 0)
            {
                currentGear = -1;
            }
        }

        acceleration = 0;

        float targetSpeedMul = Mathf.Abs(verticalAxis);
        if (targetSpeedMul != 0)
        {
            var currentSpeed = chassis.CurrentSpeed; //TODO: we probably should use projection on forward vector, but as it is just test script left like this?
            acceleration = acceleratorPID.Update(chassis.TargetSpeed * targetSpeedMul - currentSpeed, Time.fixedDeltaTime);
        }

        braking = 0;
        if (spaceIsPressed)
        {
            acceleration = 0;
            braking = 1;
        }

        // Apply
        if (engine != null)
        {
            engine.Accelerator = acceleration;
            engine.Gear = currentGear;
        }

        if (chassis != null)
        {
            chassis.BrakingState  = braking;
            chassis.SteeringState = steering;
        }
    }
}
