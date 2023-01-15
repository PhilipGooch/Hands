using System;
using UnityEngine;
using Node = UnityEngine.XR.XRNode;

#if OCULUSVR || UNITY_EDITOR

namespace VR.System
{
    public class OculusVRData : MonoBehaviour, IVRDataProvider
    {
        [SerializeField]
        Transform leftPose;
        [SerializeField]
        Transform rightPose;
        [SerializeField]
        Transform leftHandTransform;
        [SerializeField]
        Transform rightHandTransform;
        [SerializeField]
        Transform leftModel;
        [SerializeField]
        Transform rightModel;
        [SerializeField]
        Collider leftControllerBounds;
        [SerializeField]
        Collider rightControllerBounds;

        Transform cameraAnchor;
        OVRPlugin.Controller activeControllers = OVRPlugin.Controller.None;
        float[] vibrationTimer = new float[2];
        const float minIndexCurlOnTrigger = 0.25f;
        const float minThumbCurlOnTouchpad = 0.5f;

        public bool Initialized => OVRManager.OVRManagerinitialized;

        public bool IsStartupFrame => false;

        public bool Calibrating => false;

        public bool NeedsHeightCalibration => false;

        // Oculus runs at 72fps
        public float FixedDeltaTime => 1f / 72f;
        const int velocityFrameCount = 12;

        public event Action HeadsetTrackingStarted;
        public event Action ModelLoaded;
        public event Action<HandDirection, bool> HandConnectionChanged;

        PreviousVelocityData[] previousLinearVelocities = new PreviousVelocityData[2] { new PreviousVelocityData(velocityFrameCount), new PreviousVelocityData(velocityFrameCount) };
        PreviousVelocityData[] previousAngularVelocities = new PreviousVelocityData[2] { new PreviousVelocityData(velocityFrameCount), new PreviousVelocityData(velocityFrameCount) };

        public void GetBounds(out Vector3 firstCorner, out Vector3 secondCorner)
        {
            var dimensions = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
            firstCorner = -dimensions / 2f;
            secondCorner = dimensions / 2f;
        }

        public void GetEstimatedPeakVelocities(HandDirection handDir, out Vector3 velocity, out Vector3 angular)
        {
            // TODO: implement proper peak velocity calculation
            velocity = previousLinearVelocities[(int)handDir].PeakVelocity;
            angular = previousAngularVelocities[(int)handDir].PeakVelocity;
        }

        public Vector3 GetHandAngularVelocity(HandDirection handDir)
        {
            return -OVRInput.GetLocalControllerAngularVelocity(GetController(handDir));
        }

        public Vector3 GetHandVelocity(HandDirection handDir)
        {
            return OVRInput.GetLocalControllerVelocity(GetController(handDir));
        }

        public Transform GetModel(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftModel : rightModel;
        }

        public Transform GetPoseTransform(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftHandTransform : rightHandTransform;
        }

        public Collider GetControllerBounds(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftControllerBounds : rightControllerBounds;
        }

        public bool ReadInputFromDevice(HandDirection handDir, HandInputType inputType)
        {
            var controller = GetController(handDir);
            switch (inputType)
            {
                case HandInputType.triggerDown:
                    return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller);
                case HandInputType.triggerUp:
                    return OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, controller);
                case HandInputType.grabDown:
                    return OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller);
                case HandInputType.grabUp:
                    return OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller);
                case HandInputType.trackpadDown:
                    return OVRInput.GetDown(OVRInput.Touch.PrimaryThumbRest, controller);
                case HandInputType.trackpadUp:
                    return OVRInput.GetUp(OVRInput.Touch.PrimaryThumbRest, controller);
                case HandInputType.aButtonDown:
                    return OVRInput.GetDown(OVRInput.Button.One, controller);
                case HandInputType.bButtonDown:
                    return OVRInput.GetDown(OVRInput.Button.Two, controller);
            }
            return false;
        }

        public Vector2 ReadMoveInput(HandDirection handDir)
        {
            var controller = GetController(handDir);
            return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller);
        }

        public float ReadTriggerValue(HandDirection handDir)
        {
            var controller = GetController(handDir);
            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        }

        public void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude, HandDirection handDir)
        {
            var controller = GetController(handDir);
            vibrationTimer[(int)handDir] = Mathf.Max(vibrationTimer[(int)handDir], duration);
            OVRInput.SetControllerVibration(vibrationFrequency / 320f, amplitude, controller);
        }

        OVRInput.Controller GetController(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        }

        // Start is called before the first frame update
        void Awake()
        {
            cameraAnchor = VRSystem.Instance.mainCamera.transform;
            Application.onBeforeRender += OnBeforeRenderCallback;
            OVRManager.fixedFoveatedRenderingLevel = OVRManager.FixedFoveatedRenderingLevel.High;
        }

        void Update()
        {
            UpdateVibration();
        }

        private void FixedUpdate()
        {
            UpdateConnection();
            UpdateAnchors(true, true);
            UpdatePeakVelocities();
        }

        protected virtual void OnBeforeRenderCallback()
        {
            if (OVRManager.loadedXRDevice == OVRManager.XRDevice.Oculus)            //Restrict late-update to only Oculus devices
            {
                bool controllersNeedUpdate = OVRManager.instance.LateControllerUpdate;
#if USING_XR_SDK
			//For the XR SDK, we need to late update head pose, not just the controllers, because the functionality
			//is no longer built-in to the Engine. Under legacy, late camera update is done by default. In the XR SDK, you must use
			//Tracked Pose Driver to get this by default, which we do not use. So, we have to manually late update camera poses.
			UpdateAnchors(true, controllersNeedUpdate);
#else
                if (controllersNeedUpdate)
                    UpdateAnchors(false, true);
#endif
            }
        }

        void UpdatePeakVelocities()
        {
            for (int i = 0; i < 2; i++)
            {
                previousLinearVelocities[i].AddVelocity(GetHandVelocity((HandDirection)i));
                previousAngularVelocities[i].AddVelocity(GetHandAngularVelocity((HandDirection)i));
            }
        }

        void UpdateConnection()
        {
            var currentState = OVRPlugin.GetActiveController();
            var diff = currentState ^ activeControllers;
            if ((diff & OVRPlugin.Controller.LTouch) > 0)
            {
                HandConnectionChanged(HandDirection.Left, (currentState & OVRPlugin.Controller.LTouch) > 0);
            }
            if ((diff & OVRPlugin.Controller.RTouch) > 0)
            {
                HandConnectionChanged(HandDirection.Right, (currentState & OVRPlugin.Controller.RTouch) > 0);
            }

            activeControllers = currentState;
        }

        protected virtual void UpdateAnchors(bool updateEyeAnchors, bool updateHandAnchors)
        {
            if (!OVRManager.OVRManagerinitialized)
                return;

            if (!Application.isPlaying)
                return;

            /*if (_skipUpdate)
            {
                cameraAnchor.FromOVRPose(OVRPose.identity, true);

                return;
            }*/

            bool hmdPresent = OVRNodeStateProperties.IsHmdPresent();

            OVRPose tracker = OVRManager.tracker.GetPose();

            //trackerAnchor.localRotation = tracker.orientation;

            Quaternion emulatedRotation = Quaternion.Euler(-OVRManager.instance.headPoseRelativeOffsetRotation.x, -OVRManager.instance.headPoseRelativeOffsetRotation.y, OVRManager.instance.headPoseRelativeOffsetRotation.z);

            //Note: in the below code, when using UnityEngine's API, we only update anchor transforms if we have a new, fresh value this frame.
            //If we don't, it could mean that tracking is lost, etc. so the pose should not change in the virtual world.
            //This can be thought of as similar to calling InputTracking GetLocalPosition and Rotation, but only for doing so when the pose is valid.
            //If false is returned for any of these calls, then a new pose is not valid and thus should not be updated.
            if (updateEyeAnchors)
            {
                if (hmdPresent)
                {
                    Vector3 centerEyePosition = Vector3.zero;
                    Quaternion centerEyeRotation = Quaternion.identity;

                    if (OVRNodeStateProperties.GetNodeStatePropertyVector3(Node.CenterEye, NodeStatePropertyType.Position, OVRPlugin.Node.EyeCenter, OVRPlugin.Step.Render, out centerEyePosition))
                        cameraAnchor.localPosition = centerEyePosition;
                    if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(Node.CenterEye, NodeStatePropertyType.Orientation, OVRPlugin.Node.EyeCenter, OVRPlugin.Step.Render, out centerEyeRotation))
                        cameraAnchor.localRotation = centerEyeRotation;
                }

                Vector3 leftEyePosition = Vector3.zero;
                Vector3 rightEyePosition = Vector3.zero;
                Quaternion leftEyeRotation = Quaternion.identity;
                Quaternion rightEyeRotation = Quaternion.identity;
            }

            // Don't update hand anchors if we don't have input focus
            // Otherwise the anchors will get teleported to some weird location like the head or feet of the player
            if (updateHandAnchors && OVRManager.hasInputFocus)
            {
                //Need this for controller offset because if we're on OpenVR, we want to set the local poses as specified by Unity, but if we're not, OVRInput local position is the right anchor
                if (OVRManager.loadedXRDevice == OVRManager.XRDevice.OpenVR)
                {
                    Vector3 leftPos = Vector3.zero;
                    Vector3 rightPos = Vector3.zero;
                    Quaternion leftQuat = Quaternion.identity;
                    Quaternion rightQuat = Quaternion.identity;

                    if (OVRNodeStateProperties.GetNodeStatePropertyVector3(Node.LeftHand, NodeStatePropertyType.Position, OVRPlugin.Node.HandLeft, OVRPlugin.Step.Render, out leftPos))
                        leftPose.localPosition = leftPos;
                    if (OVRNodeStateProperties.GetNodeStatePropertyVector3(Node.RightHand, NodeStatePropertyType.Position, OVRPlugin.Node.HandRight, OVRPlugin.Step.Render, out rightPos))
                        rightPose.localPosition = rightPos;
                    if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(Node.LeftHand, NodeStatePropertyType.Orientation, OVRPlugin.Node.HandLeft, OVRPlugin.Step.Render, out leftQuat))
                        leftPose.localRotation = leftQuat;
                    if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(Node.RightHand, NodeStatePropertyType.Orientation, OVRPlugin.Node.HandRight, OVRPlugin.Step.Render, out rightQuat))
                        rightPose.localRotation = rightQuat;

                }
                else
                {
                    leftPose.localPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
                    rightPose.localPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                    leftPose.localRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
                    rightPose.localRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
                }

                //trackerAnchor.localPosition = tracker.position;

                OVRPose leftOffsetPose = OVRPose.identity;
                OVRPose rightOffsetPose = OVRPose.identity;
                /*if (OVRManager.loadedXRDevice == OVRManager.XRDevice.OpenVR)
                {
                    leftOffsetPose = OVRManager.GetOpenVRControllerOffset(Node.LeftHand);
                    rightOffsetPose = OVRManager.GetOpenVRControllerOffset(Node.RightHand);

                    //Sets poses of left and right nodes, local to the tracking space.
                    OVRManager.SetOpenVRLocalPose(trackingSpace.InverseTransformPoint(leftControllerAnchor.position),
                        trackingSpace.InverseTransformPoint(rightControllerAnchor.position),
                        Quaternion.Inverse(trackingSpace.rotation) * leftControllerAnchor.rotation,
                        Quaternion.Inverse(trackingSpace.rotation) * rightControllerAnchor.rotation);
                }*/

                // MODELS UPDATED VIA HAND.cs
                //rightModel.localPosition = rightOffsetPose.position;
                //rightModel.localRotation = rightOffsetPose.orientation;
                //leftModel.localPosition = leftOffsetPose.position;
                //leftModel.localRotation = leftOffsetPose.orientation;
            }
        }

        void UpdateVibration()
        {
            // Oculus does not track vibration time so we must stop the vibrations ourselves
            for (int i = 0; i < vibrationTimer.Length; i++)
            {
                // Make sure we vibrate for at least one frame, otherwise nothing will happen
                if (vibrationTimer[i] > 0f)
                {
                    vibrationTimer[i] -= Time.deltaTime;
                    // Zero indicates that the vibration was stopped.
                    // Ensure that we don't set this to zero by accident before vibration has been stopped.
                    if (vibrationTimer[i] == 0f)
                    {
                        vibrationTimer[i] = -1f;
                    }
                }
                else if (vibrationTimer[i] < 0f)
                {
                    vibrationTimer[i] = 0f;
                    Vibrate(0f, 0f, 0, 0f, (HandDirection)i);
                }
            }
        }

        public void AdjustPlayerPositionBasedOnHeight(Transform playerTransform, float playerHeight, Vector3 startPos, float playAreaScale)
        {
        }

        public float GetGrabAmount(HandDirection handDir)
        {
            return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, GetController(handDir));
        }

        public float GetFingerCurl(HandDirection handDir, Finger finger)
        {
            var controller = GetController(handDir);
            switch (finger)
            {
                case Finger.Index:
                    var triggerInput = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
                    bool fingerIsOnTrigger = OVRInput.Get(OVRInput.NearTouch.PrimaryIndexTrigger, controller);
                    if (fingerIsOnTrigger)
                    {
                        return Mathf.Lerp(minIndexCurlOnTrigger, 1f, triggerInput);
                    }
                    else
                    {
                        return 0f;
                    }
                case Finger.Thumb:
                    return OVRInput.Get(OVRInput.NearTouch.PrimaryThumbButtons, controller) ? minThumbCurlOnTouchpad : 0f;
                case Finger.Ring:
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
            }
            return 0f;
        }
    }
}

#endif

