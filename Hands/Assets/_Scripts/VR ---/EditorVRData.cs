using System;
using UnityEngine;

namespace VR.System
{
    public class EditorVRData : MonoBehaviour, IVRDataProvider
    {
        [Header("Hands")]

        [SerializeField]
        private Collider leftHandCollider;
        [SerializeField]
        private Collider rightHandCollider;
        [SerializeField]
        private Transform leftPoseTransform;
        [SerializeField]
        private Transform rightPoseTransform;
        [SerializeField]
        private Transform leftHandModel;
        [SerializeField]
        private Transform rightHandModel;

        [Header("Camera Movement")]
        [SerializeField]
        float moveSpeed = 2;
        [SerializeField]
        float rotationSpeed = 3f;
        [SerializeField]
        float pitch = 50;

        private float yaw;
        private Transform cameraRig;
        private Transform cameraMain;

        public bool Initialized => true;

        public bool IsStartupFrame => false;

        public bool Calibrating => false;

        public bool NeedsHeightCalibration => false;

        public float FixedDeltaTime => 1 / 72f;

        public event Action HeadsetTrackingStarted;
        public event Action ModelLoaded;
        public event Action<HandDirection, bool> HandConnectionChanged;

        private Vector3 lastHandPos;
        private Quaternion lastHandRot;

        private Vector3 leftHandVelocity = Vector3.zero;
        private Vector3 leftHandAngularVelocity = Vector3.zero;
        private float distanceFromCamera;

        const int velocityFrameCount = 12;

        private PreviousVelocityData[] previousLinearVelocities = new PreviousVelocityData[2] { new PreviousVelocityData(velocityFrameCount), new PreviousVelocityData(velocityFrameCount) };
        private PreviousVelocityData[] previousAngularVelocities = new PreviousVelocityData[2] { new PreviousVelocityData(velocityFrameCount), new PreviousVelocityData(velocityFrameCount) };

        void Start()
        {
            //move unused hand away to not interfere
            rightPoseTransform.position = new Vector3(100, 100, 100);
        }

        private void OnEnable()
        {
            cameraMain = Camera.main.transform;
            cameraRig = cameraMain.parent;
        }

        private void Update()
        {
            MoveHeadset();
            UpdateHandTransform(HandDirection.Left);
        }

        void FixedUpdate()
        {
            UpdateLeftHandAngularVelocity();
            UpdateLeftHandLinearVelocity();
            UpdatePeakVelocities();
            UpdatePoseAndModelTransforms(HandDirection.Left);
        }

        void UpdatePoseAndModelTransforms(HandDirection handDir)
        {
            var modelTransform = GetModel(handDir);
            var poseTransform = GetPoseTransform(handDir);

            modelTransform.position = poseTransform.position = targetHandPos;
            modelTransform.rotation = poseTransform.rotation = targetHandRotation;
        }

        //using SteamVR values
        public void GetBounds(out Vector3 firstCorner, out Vector3 secondCorner)
        {
            firstCorner = new Vector3(-1.5f, 0, -1.1f);
            secondCorner = new Vector3(1.5f, 0, 1.1f);
        }

        //Irrelevant, used to detect if controllers are close
        public Collider GetControllerBounds(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftHandCollider : rightHandCollider;
        }

        public void GetEstimatedPeakVelocities(HandDirection handDir, out Vector3 velocity, out Vector3 angular)
        {
            velocity = previousLinearVelocities[(int)handDir].PeakVelocity;
            angular = previousAngularVelocities[(int)handDir].PeakVelocity;
        }

        public float GetFingerCurl(HandDirection handDir, Finger finger)
        {
            if (handDir == HandDirection.Left)
            {
                return Input.GetMouseButton(0) ? 1 : 0;
            }

            return 0;
        }

        public float GetGrabAmount(HandDirection handDir)
        {
            if (handDir == HandDirection.Left)
            {
                return Input.GetMouseButton(0) ? 1 : 0;
            }

            return 0;
        }

        public Vector3 GetHandAngularVelocity(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftHandAngularVelocity : Vector3.zero;
        }

        //SOOOO, just FYI, velocity has to be local, not global
        public Vector3 GetHandVelocity(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftHandVelocity : Vector3.zero;
        }

        public Transform GetModel(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftHandModel : rightHandModel;
        }

        private void MoveHeadset()
        {
            var moveRot = Quaternion.Euler(pitch, yaw, 0);
            var speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) speed *= 2;
            if (Input.GetKey(KeyCode.W)) cameraRig.position += moveRot * Vector3.forward * Time.fixedDeltaTime * speed;
            if (Input.GetKey(KeyCode.S)) cameraRig.position -= moveRot * Vector3.forward * Time.fixedDeltaTime * speed;
            if (Input.GetKey(KeyCode.A)) cameraRig.position -= moveRot * Vector3.right * Time.fixedDeltaTime * speed;
            if (Input.GetKey(KeyCode.D)) cameraRig.position += moveRot * Vector3.right * Time.fixedDeltaTime * speed;
            if (Input.GetKey(KeyCode.Q)) cameraRig.position += moveRot * Vector3.up * Time.fixedDeltaTime * speed;
            if (Input.GetKey(KeyCode.Z)) cameraRig.position -= moveRot * Vector3.up * Time.fixedDeltaTime * speed;

            if (Input.GetMouseButton(1))
            {
                var originalCameraPosition = cameraMain.position;

                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * rotationSpeed, -90f, 90f);
                yaw += Input.GetAxis("Mouse X") * rotationSpeed;

                // Calculate the quaternion to rotate the camera by pitch + yaw via parent
                var euler = Quaternion.Euler(pitch, yaw, 0f);
                var childInverse = Quaternion.Inverse(cameraMain.rotation);
                cameraRig.rotation = euler * childInverse * cameraRig.rotation;

                // Put camera back in the same position
                var incorrectCameraPosition = cameraMain.position;
                var diff = incorrectCameraPosition - originalCameraPosition;
                cameraRig.position -= diff;
            }

            if (!Input.GetMouseButton(0))
            {
                cameraRig.position += moveRot * Vector3.forward * Input.mouseScrollDelta.y;
            }
        }

        private void UpdateLeftHandAngularVelocity()
        {
            Quaternion deltaRotation = leftPoseTransform.rotation * Quaternion.Inverse(lastHandRot);
            Vector3 eulerRotation = new Vector3(
                Mathf.DeltaAngle(0, Mathf.Round(deltaRotation.eulerAngles.x)),
                Mathf.DeltaAngle(0, Mathf.Round(deltaRotation.eulerAngles.y)),
                Mathf.DeltaAngle(0, Mathf.Round(deltaRotation.eulerAngles.z)));

            leftHandAngularVelocity = eulerRotation / Time.fixedDeltaTime * Mathf.Deg2Rad;
            lastHandRot = leftPoseTransform.rotation;
        }

        private void UpdateLeftHandLinearVelocity()
        {
            leftHandVelocity = (leftPoseTransform.localPosition - lastHandPos) / Time.fixedDeltaTime;
            lastHandPos = leftPoseTransform.localPosition;
        }

        private void UpdatePeakVelocities()
        {
            for (int i = 0; i < 2; i++)
            {
                var vel = GetHandVelocity((HandDirection)i);
                var angVel = GetHandAngularVelocity((HandDirection)i);
                previousLinearVelocities[i].AddVelocity(vel);
                previousAngularVelocities[i].AddVelocity(angVel);
            }
        }

        Vector3 targetHandPos;
        Quaternion targetHandRotation;

        private void UpdateHandTransform(HandDirection handDir)
        {
            var toUpdate = GetPoseTransform(handDir);
            var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            targetHandPos = toUpdate.position;

            if (Physics.Raycast(mouseRay, out var hit, 100, (int)(1 << 0 | 1 << 13 | 1 << 14 | 1 << 17)))
            {
                if (Input.GetMouseButtonDown(0))
                {
                    distanceFromCamera = hit.distance;
                }
                targetHandPos = hit.point;
            }

            if (Input.GetMouseButton(0))
            {
                distanceFromCamera = Mathf.Clamp(distanceFromCamera + Input.mouseScrollDelta.y, 1, 100);
                targetHandPos = transform.position + mouseRay.direction * distanceFromCamera;
            }

            targetHandRotation = Quaternion.LookRotation(mouseRay.direction) * Quaternion.Euler(-90, 0, 0);
        }

        public Transform GetPoseTransform(HandDirection handDir)
        {
            return handDir == HandDirection.Left ? leftPoseTransform : rightPoseTransform;
        }

        public bool ReadInputFromDevice(HandDirection handDir, HandInputType inputType)
        {
            switch (inputType)
            {
                case HandInputType.triggerDown:
                    return Input.GetKeyDown(KeyCode.Space);
                case HandInputType.triggerUp:
                    return Input.GetKeyUp(KeyCode.Space);
                case HandInputType.grabDown:
                    return Input.GetMouseButtonDown(0);
                case HandInputType.grabUp:
                    return Input.GetMouseButtonUp(0);
                case HandInputType.trackpadDown:
                    break;
                case HandInputType.trackpadUp:
                    break;
                case HandInputType.aButtonDown:
                    if (handDir == HandDirection.Left)
                        return Input.GetKeyDown(KeyCode.F11);
                    else if (handDir == HandDirection.Right)
                        return Input.GetKeyDown(KeyCode.Return);
                    break;
                case HandInputType.bButtonDown:
                    return Input.GetKeyDown(KeyCode.Escape);
                default:
                    break;
            }

            return false;
        }

        //used to thumbsticks value, in this case maybe it can be used to get mouse movement delta
        public Vector2 ReadMoveInput(HandDirection handDir)
        {
            return Vector2.zero;
        }

        public float ReadTriggerValue(HandDirection handDir)
        {
            return Input.GetKey(KeyCode.Space) ? 1 : 0;
        }

        public void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude, HandDirection handDir) { }
        public void AdjustPlayerPositionBasedOnHeight(Transform playerTransform, float playerHeight, Vector3 startPos, float playAreaScale) { }
    }
}
