//#define DEBUG_CONTROLS

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using VR.System;

public class Player : MonoBehaviour
{
    [SerializeField]
    public PlayerUIManager playerUIManager;

    public Move move;
    public VRLocomotion vrLoco;
    public Teleportation teleportation;

    public Hand leftHand;
    public Hand rightHand;
    public Camera mainCamera;
    [HideInInspector]
    public Hand mainHand;
    public bool LeftEyeUnderwater { get; private set; } = false;
    public bool RightEyeUnderwater { get; private set; } = false;
    protected static int lastFrameCount = -1;

    Vector3 levelStartPosition;

    public UnderwaterParameters UnderwaterParameters
    {
        get; private set;
    }

    public static bool Initialized
    {
        get
        {
            return VRSystem.Instance.Initialized && !VRSystem.Instance.IsStartupFrame;
        }
    }

    public static Player Instance { get; private set; }
    public Player Initialize()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            mainHand = rightHand;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
        SetControllerMovementEnabled(GameSettings.Instance.locomotionMode.Value == (int)LocomotionMode.JOYSTICK);
        SetCameraRotationMode(GameSettings.Instance.CamRotationMode);
        SetCameraMovementMode(GameSettings.Instance.CamMovementMode);
        SetInstantCameraAnimations(GameSettings.Instance.instantCameraAnimations.Value);
        SetTeleportationEnabled(GameSettings.Instance.locomotionMode.Value == (int)LocomotionMode.TELEPORTATION);

        return Instance;
    }

    private void OnEnable()
    {
        Instance.levelStartPosition = transform.position;

        if (Instance != this)
        {
            AdjustPositionToThisPosition(Instance);
            DestroyImmediate(gameObject);
        }
        else
        {
            AdjustPlayerHeight(this);
        }
    }

    public void SetControllerMovementEnabled(bool movementEnabled)
    {
        vrLoco.LimitedMovement = !movementEnabled;
        move.LimitedMovement = !movementEnabled;
    }

    public void SetControllerRotationEnabled(bool rotationEnabled)
    {
        move.LimitedRotation = !rotationEnabled;
    }

    public void SetCameraRotationMode(CameraRotationMode rotationMode)
    {
        move.SetRotationMode(rotationMode);
    }

    public void SetCameraMovementMode(CameraMovementMode movementMode)
    {
        move.SetMovementMode(movementMode);
    }

    public void SetTeleportationEnabled(bool teleportationEnabled)
    {
        teleportation.ShouldTeleport = teleportationEnabled;
    }

    public void SetInstantCameraAnimations(bool instantAnimations)
    {
        move.InstantAnimations = instantAnimations;
    }

    public void StopGrabbingObject(Rigidbody body)
    {
        if (mainHand.attachedBody == body)
        {
            mainHand.ReleaseGrabbedObject();
        }
        if (mainHand.otherHand.attachedBody == body)
        {
            mainHand.otherHand.ReleaseGrabbedObject();
        }
    }

    public Hand GetHandThatIsGrabbingBody(Rigidbody body)
    {
        if (mainHand.attachedBody == body)
        {
            return mainHand;
        }
        if (mainHand.otherHand.attachedBody == body)
        {
            return mainHand.otherHand;
        }
        return null;
    }

    public void GetEyePositions(out Vector3 leftPos, out Vector3 rightPos)
    {
        var cameraPos = mainCamera.transform.position;
        var separation = mainCamera.stereoSeparation;
        var offset = mainCamera.transform.right * separation * 0.5f * PlayArea.scale;
        leftPos = cameraPos - offset;
        rightPos = cameraPos + offset;
    }

    public void UpdateEyeUnderwaterState(bool leftUnderwater, bool rightUnderwater, UnderwaterParameters underwaterParameters)
    {
        if (leftUnderwater)
            LeftEyeUnderwater = true;
        if (rightUnderwater)
            RightEyeUnderwater = true;
        if (leftUnderwater || rightUnderwater)
            UnderwaterParameters = underwaterParameters;
    }

    public void ReadjustPlayerInstancePosition()
    {
        AdjustPositionToThisPosition(Instance);
    }

    void AdjustPositionToThisPosition(Player target)
    {
        target.transform.position = transform.position;
        target.transform.rotation = transform.rotation;

        target.vrLoco.ResetPosition();

        AdjustPlayerHeight(target);

    }

    public void CalibrateHeight()
    {
        var diff = mainCamera.transform.position - vrLoco.transform.position;
        var heightDiff = diff.y;
        var presumedPlayerHeight = heightDiff / PlayArea.scale;
        GameSettings.Instance.playerHeight.Value = presumedPlayerHeight * 100f;
    }
    public void AdjustPlayerHeight()
    {
        AdjustPlayerHeight(Instance);

    }

    void AdjustPlayerHeight(Player target)
    {
        var playerHeight = GameSettings.Instance.playerHeight.Value / 100f;
        VRSystem.Instance.AdjustPlayerPositionBasedOnHeight(target.transform, playerHeight, Instance.levelStartPosition, PlayArea.scale);
    }

    void ReadPose(HandDirection handDir, out RigidTransform transform, out Vector6 velocity)
    {
        var pose = VRSystem.Instance.GetPoseTransform(handDir);
        transform = new RigidTransform(pose.rotation, pose.position);
        velocity = new Vector6(VRSystem.Instance.GetHandAngularVelocity(handDir), VRSystem.Instance.GetHandVelocity(handDir) * PlayArea.scale);
    }

    private void FixedUpdate()
    {
        ReadPose(leftHand.handDirection, out var posL, out var velL);
        ReadPose(rightHand.handDirection, out var posR, out var velR);
        vrLoco.OnFixedStep(ref posL, ref velL, ref posR, ref velR);
        leftHand.ReadPose(posL.pos, posL.rot, velL.linear, velL.angular);
        rightHand.ReadPose(posR.pos, posR.rot, velR.linear, velR.angular);

        if (rightHand.TwoHandMaster)
        {
            rightHand.OnFixedStep();
            leftHand.OnFixedStep();
        }
        else
        {
            leftHand.OnFixedStep();
            rightHand.OnFixedStep();
        }

        leftHand.FlushFixedUpdateInputs();
        rightHand.FlushFixedUpdateInputs();
    }

    private void LateUpdate()
    {
        if (!Initialized)
            return;

        leftHand.OnLateUpdate();
        rightHand.OnLateUpdate();
    }

    private void Update()
    {
        //if (rightHand.GetInput(HandInputType.aButtonDown)) { Restart(); StartCoroutine(LevelManager.LoadNextLevel()); }
        //if (rightHand.GetInput(HandInputType.bButtonDown)) { Restart(); StartCoroutine(LevelManager.LoadPreviousLevel()); }
        
        if (leftHand.GetInput(HandInputType.bButtonDown) || rightHand.GetInput(HandInputType.bButtonDown))
        {
            if (!LevelManager.Instance.InMainMenu())
            {
                if (LevelFinish.levelWon == false)
                {
                    if (!PlayerUIManager.Instance)
                    {
                        Instantiate(playerUIManager);
                    }
                    PlayerUIManager.Instance.ShowPanel(MenuState.PAUSE);
                }
            }
        }
#if DEBUG_CONTROLS
        if (Input.GetKeyDown(KeyCode.B))
        {
             PlayerUIManager.Instance.ShowPanel(MenuState.LEVEL_DONE);
        }
#endif
        UpdateMainHand();
        LeftEyeUnderwater = false;
        RightEyeUnderwater = false;
    }

    void UpdateMainHand()
    {
        const float triggerThreshold = 0.25f;
        if (mainHand.Trigger < triggerThreshold && mainHand.otherHand.Trigger > triggerThreshold)
        {
            mainHand = mainHand.otherHand;
        }
    }

    private void Restart()
    {
        leftHand.Restart();
        rightHand.Restart();
    }
}
