using System;
using UnityEngine;
#if STEAMVR || UNITY_EDITOR
using Valve.VR;

namespace VR.System
{
    public class SteamVRData : MonoBehaviour, IVRDataProvider
    {
        [SerializeField]
        Transform leftPoseTransform;
        [SerializeField]
        Transform rightPoseTransform;
        [SerializeField]
        SteamVR_Behaviour_Pose leftHandPose;
        [SerializeField]
        SteamVR_Behaviour_Pose rightHandPose;
        [SerializeField]
        Transform leftHandModel;
        [SerializeField]
        Transform rightHandModel;
        [SerializeField]
        Collider leftControllerBounds;
        [SerializeField]
        Collider rightControllerBounds;

        public SteamVR_Action_Boolean triggerAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Trigger");
        public SteamVR_Action_Single triggerAnalogAction = SteamVR_Input.GetAction<SteamVR_Action_Single>("TriggerAnalog");
        public SteamVR_Action_Boolean grabAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("Grip");
        public SteamVR_Action_Vector2 moveAction = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("Move");
        public SteamVR_Action_Boolean trackpadTouchAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("TrackpadTouch");
        public SteamVR_Action_Vibration hapticAction = SteamVR_Input.GetAction<SteamVR_Action_Vibration>("Haptic");
        public SteamVR_Action_Boolean aButtonAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("AButton");
        public SteamVR_Action_Boolean bButtonAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("BButton");
        public SteamVR_Action_Skeleton skeletonLeftAction;
        public SteamVR_Action_Skeleton skeletonRightAction;

        public bool Initialized => SteamVR.initializedState == SteamVR.InitializedStates.InitializeSuccess;

        public bool Calibrating => SteamVR.calibrating || SteamVR.outOfRange;

        public bool IsStartupFrame => SteamVR_Input.isStartupFrame;

        public bool NeedsHeightCalibration => true;
 
        public float FixedDeltaTime => 1f / 72f;

        public event Action HeadsetTrackingStarted;
        public event Action ModelLoaded;
        public event Action<HandDirection, bool> HandConnectionChanged;

        SteamVR_Events.Action renderModelLoadedAction;

        void Start()
        {
            VRSystem.Instance.mainCamera.gameObject.AddComponent<SteamVR_CameraHelper>();
            SteamVR_Events.System(EVREventType.VREvent_TrackedDeviceUserInteractionStarted).Listen(TrackingStarted);
            renderModelLoadedAction = SteamVR_Events.RenderModelLoadedAction(OnRenderModelLoaded);
            renderModelLoadedAction.enabled = true;
            // These don't work unless initialized after entering play mode
            skeletonLeftAction = SteamVR_Input.GetSkeletonAction("SkeletonLeft");
            skeletonRightAction = SteamVR_Input.GetSkeletonAction("SkeletonRight");
            leftHandPose.onConnectedChangedEvent += (action, source, connected) => { HandConnectionChanged?.Invoke(HandDirection.Left, connected); };
            rightHandPose.onConnectedChangedEvent += (action, source, connected) => { HandConnectionChanged?.Invoke(HandDirection.Right, connected); };
        }

        public bool ReadInputFromDevice(HandDirection handDir, HandInputType inputType)
        {
            var pose = GetPose(handDir);

            switch (inputType)
            {
                case HandInputType.triggerDown:
                    return triggerAction.GetStateDown(pose.inputSource);
                case HandInputType.triggerUp:
                    return triggerAction.GetStateUp(pose.inputSource);
                case HandInputType.grabDown:
                    return grabAction.GetStateDown(pose.inputSource);
                case HandInputType.grabUp:
                    return grabAction.GetStateUp(pose.inputSource);
                case HandInputType.trackpadDown:
                    return trackpadTouchAction.GetStateDown(pose.inputSource);
                case HandInputType.trackpadUp:
                    return trackpadTouchAction.GetStateUp(pose.inputSource);
                case HandInputType.aButtonDown:
                    return aButtonAction.GetStateDown(pose.inputSource);
                case HandInputType.bButtonDown:
                    return bButtonAction.GetStateDown(pose.inputSource);
            }
            return false;
        }

        public float ReadTriggerValue(HandDirection handDir)
        {
            return triggerAnalogAction.GetAxis(GetPose(handDir).inputSource);
        }

        public Vector2 ReadMoveInput(HandDirection handDir)
        {
            return moveAction.GetAxis(GetPose(handDir).inputSource);
        }

        SteamVR_Behaviour_Pose GetPose(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftHandPose : rightHandPose;
        }

        public void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude, HandDirection handDir)
        {
            hapticAction.Execute(secondsFromNow, duration, vibrationFrequency, amplitude, GetPose(handDir).inputSource);
        }

        public Transform GetModel(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftHandModel : rightHandModel;
        }

        public Transform GetPoseTransform(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftPoseTransform : rightPoseTransform;
        }

        public Collider GetControllerBounds(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftControllerBounds : rightControllerBounds;
        }

        public void GetEstimatedPeakVelocities(HandDirection handDir, out Vector3 velocity, out Vector3 angular)
        {
            GetPose(handDir).GetEstimatedPeakVelocities(out velocity, out angular);
        }

        public Vector3 GetHandVelocity(HandDirection handDir)
        {
            return GetPose(handDir).GetVelocity();
        }

        public Vector3 GetHandAngularVelocity(HandDirection handDir)
        {
            return GetPose(handDir).GetAngularVelocity();
        }

        public void GetBounds(out Vector3 firstCorner, out Vector3 secondCorner)
        {
            var rect = new HmdQuad_t();
            if (!SteamVR_PlayArea.GetBounds(SteamVR_PlayArea.Size._300x225, ref rect))
            {
                firstCorner = Vector3.zero;
                secondCorner = Vector3.zero;
                return;
            }
            firstCorner = new Vector3(rect.vCorners1.v0, rect.vCorners1.v1, rect.vCorners1.v2);
            secondCorner = new Vector3(rect.vCorners3.v0, rect.vCorners3.v1, rect.vCorners3.v2);
        }

        void TrackingStarted(VREvent_t arg0)
        {
            HeadsetTrackingStarted?.Invoke();
        }

        void OnRenderModelLoaded(SteamVR_RenderModel renderModel, bool succeess)
        {
            if (renderModel.index != SteamVR_TrackedObject.EIndex.Hmd)
            {
                ModelLoaded?.Invoke();
            }
        }

        void Update()
        {
            if (SteamVR_Settings.instance.poseUpdateMode != SteamVR_UpdateModes.OnFixedUpdate)
            {
                throw new Exception("Incorrect steam vr pose update mode. Should be OnFixedUpdate!!!");
            }
        }

        public void AdjustPlayerPositionBasedOnHeight(Transform playerTransform, float playerHeight, Vector3 startPos, float playAreaScale)
        {

            // The player transform position is actually the floor, but the head is not on the floor
            // if we want the camera to be at the player transform position, we have to shift everything down
            var scaledHeight = playerHeight * playAreaScale;
            Vector3 adjusted = new Vector3(
                playerTransform.position.x,
                startPos.y - scaledHeight,
                playerTransform.position.z
            );

            playerTransform.position = adjusted;
        }

        public float GetGrabAmount(HandDirection handDir)
        {
            return grabAction.GetState(GetPose(handDir).inputSource) ? 1f : 0f;
        }

        public float GetFingerCurl(HandDirection handDir, Finger finger)
        {
            var skeleton = handDir == HandDirection.Left ? skeletonLeftAction : skeletonRightAction;
            return skeleton.GetFingerCurl((int)finger);
        }
    }
}
#endif
