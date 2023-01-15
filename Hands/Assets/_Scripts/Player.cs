using Unity.Mathematics;
using UnityEngine;
using VR.System;

public class Player : MonoBehaviour
{
    public Hand leftHand;
    public Hand rightHand;
    public Camera mainCamera;
    [HideInInspector]
    public Hand mainHand;
    protected static int lastFrameCount = -1;

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

        return Instance;
    }

    private void OnEnable()
    {
        if (Instance != this)
        {
            DestroyImmediate(gameObject);
        }
    }

    public void GetEyePositions(out Vector3 leftPos, out Vector3 rightPos)
    {
        var cameraPos = mainCamera.transform.position;
        var separation = mainCamera.stereoSeparation;
        var offset = 0.5f * PlayArea.scale * separation * mainCamera.transform.right;
        leftPos = cameraPos - offset;
        rightPos = cameraPos + offset;
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
        leftHand.ReadPose(posL.pos, posL.rot, velL.linear, velL.angular);
        rightHand.ReadPose(posR.pos, posR.rot, velR.linear, velR.angular);

        leftHand.OnFixedStep();
        rightHand.OnFixedStep();

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
        UpdateMainHand();
    }

    void UpdateMainHand()
    {
        const float triggerThreshold = 0.25f;
        if (mainHand.Trigger < triggerThreshold && mainHand.otherHand.Trigger > triggerThreshold)
        {
            mainHand = mainHand.otherHand;
        }
    }
}
