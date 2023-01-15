using System;
using UnityEngine;

namespace VR.System
{
    public enum HandInputType
    {
        triggerDown,
        triggerUp,
        grabDown,
        grabUp,
        trackpadDown,
        trackpadUp,
        aButtonDown,
        bButtonDown
    }

    public enum HandDirection
    {
        Left,
        Right
    }

    public enum VRPlatform
    {
        Undefined,
        SteamVR,
        Oculus
    }

    public enum Finger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky,
    }

    public class VRSystem : MonoBehaviour, IVRDataProvider
    {
        [SerializeField]
        Transform vrParent;

        public Camera mainCamera;
        public Camera uiCamera;

        public static VRSystem Instance;
        GameObject vrSystemGameObject;

#if UNITY_EDITOR
        bool editorSystemLoaded;
        public bool EditorSystemLoaded => editorSystemLoaded;
#endif
        public enum QualityLevel
        {
            High,
            Low
        };

        public static VRPlatform CurrentVRPlatform
        {
            get
            {
#if STEAMVR
                return VRPlatform.SteamVR;
#elif OCULUSVR
                return VRPlatform.Oculus;
#else
                return VRPlatform.Undefined;
#endif
            }
        }

        // This needs to work in editor, for preprocessing assets
        public static QualityLevel GetQualityLevel()
        {
#if STEAMVR
            return QualityLevel.High;
#elif OCULUSVR
            return QualityLevel.Low;
#else
            return QualityLevel.Low;
#endif
        }

        IVRDataProvider dataProvider;

        public event Action HeadsetTrackingStarted;
        public event Action ModelLoaded;
        public event Action<HandDirection, bool> HandConnectionChanged;
        public static event Action<float> ControllerProximityChanged;

        public void Initialize()
        {
            Instance = this;
            LoadStandardDataProvider();

            string shaderQuality;
            if (!ShaderKeywordUtils.TryGetVRPlatformQualityKeyword(CurrentVRPlatform, out shaderQuality))
            {
                shaderQuality = ShaderKeywordUtils.VR_PLATFORM_DEFAULT;
                UnityEngine.Debug.LogError($"[{typeof(VRSystem)}] VR platform not recognized. Shader quality set to default." +
                    $" Please amend the platform to quality list inside \"{typeof(ShaderKeywordUtils)}\" class");
            }

            Shader.EnableKeyword(shaderQuality);
            UnityEngine.Debug.Log($"[{GetType()}] Set platform based shader quality to: {shaderQuality}");
        }

        void LoadStandardDataProvider()
        {
            string vrPrefabResource = string.Empty;
            // TODO: Strip out unused prefabs from final builds
#if STEAMVR
            vrPrefabResource = "VRData/SteamVRData";
#elif OCULUSVR
            vrPrefabResource = "VRData/OculusVRData";
#endif

            CreateDataProvider(vrPrefabResource);
        }

#if UNITY_EDITOR
        void LoadEditorDataProvider()
        {
            CreateDataProvider("VRData/EditorVRData");

            if (vrSystemGameObject != null)
                editorSystemLoaded = true;

        }
#endif

        void CreateDataProvider(string vrPrefabResource)
        {
            GameObject targetPrefab = null;
            
            if (!string.IsNullOrEmpty(vrPrefabResource))
            {
                targetPrefab = Resources.Load(vrPrefabResource) as GameObject;
            }
            if (targetPrefab != null)
            {
                if (vrSystemGameObject != null)
                    Destroy(vrSystemGameObject);

                vrSystemGameObject = Instantiate(targetPrefab, vrParent);
                dataProvider = vrSystemGameObject.GetComponent<IVRDataProvider>();
            }
            else
            {
                dataProvider = new NullDataProvider();
                UnityEngine.Debug.LogError("No VR Platform selected!");
            }

            dataProvider.HeadsetTrackingStarted += () => HeadsetTrackingStarted?.Invoke();
            dataProvider.ModelLoaded += () => ModelLoaded?.Invoke();
            dataProvider.HandConnectionChanged += (handDir, connected) => HandConnectionChanged?.Invoke(handDir, connected);
            Time.fixedDeltaTime = dataProvider.FixedDeltaTime;
        }

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (!IsStartupFrame && !Calibrating && !Initialized && !editorSystemLoaded)
            {
                LoadEditorDataProvider();
            }
#endif
            var distance = float.MaxValue;
            var leftBounds = GetControllerBounds(HandDirection.Left);
            var rightBounds = GetControllerBounds(HandDirection.Right);
            if (leftBounds && rightBounds)
            {
                var leftClosest = leftBounds.ClosestPoint(rightBounds.ClosestPoint(leftBounds.bounds.center));
                var rightClosest = rightBounds.ClosestPoint(leftClosest);
                distance = (rightClosest - leftClosest).magnitude;
            }
            ControllerProximityChanged?.Invoke(distance);
        }

        public bool Initialized => dataProvider.Initialized;
        public bool IsStartupFrame => dataProvider.IsStartupFrame;
        public bool Calibrating => dataProvider.Calibrating;
        public bool NeedsHeightCalibration => dataProvider.NeedsHeightCalibration;
        public float FixedDeltaTime => dataProvider.FixedDeltaTime;

        public bool ReadInputFromDevice(HandDirection handDir, HandInputType inputType)
        {
            return dataProvider.ReadInputFromDevice(handDir, inputType);
        }

        public float ReadTriggerValue(HandDirection handDir)
        {
            return dataProvider.ReadTriggerValue(handDir);
        }

        public Vector2 ReadMoveInput(HandDirection handDir)
        {
            return dataProvider.ReadMoveInput(handDir);
        }

        public void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude, HandDirection handDir)
        {
            dataProvider.Vibrate(secondsFromNow, duration, vibrationFrequency, amplitude, handDir);
        }

        public Transform GetModel(HandDirection handDir)
        {
            return dataProvider.GetModel(handDir);
        }
        public Transform GetPoseTransform(HandDirection handDir)
        {
            return dataProvider.GetPoseTransform(handDir);
        }

        public Collider GetControllerBounds(HandDirection handDir)
        {
            return dataProvider.GetControllerBounds(handDir);
        }

        public void GetEstimatedPeakVelocities(HandDirection handDir, out Vector3 velocity, out Vector3 angular)
        {
            dataProvider.GetEstimatedPeakVelocities(handDir, out velocity, out angular);
        }

        public Vector3 GetHandVelocity(HandDirection handDir)
        {
            return dataProvider.GetHandVelocity(handDir);
        }

        public Vector3 GetHandAngularVelocity(HandDirection handDir)
        {
            return dataProvider.GetHandAngularVelocity(handDir);
        }

        public void GetBounds(out Vector3 firstCorner, out Vector3 secondCorner)
        {
            dataProvider.GetBounds(out firstCorner, out secondCorner);
        }

        public Transform GetVRParent()
        {
            return mainCamera.transform.parent;
        }

        public void AdjustPlayerPositionBasedOnHeight(Transform playerTransform, float playerHeight, Vector3 startPos, float playAreaScale)
        {
            dataProvider.AdjustPlayerPositionBasedOnHeight(playerTransform, playerHeight, startPos, playAreaScale);
        }

        public float GetGrabAmount(HandDirection handDir)
        {
            return dataProvider.GetGrabAmount(handDir);
        }

        public float GetFingerCurl(HandDirection handDir, Finger finger)
        {
            return dataProvider.GetFingerCurl(handDir, finger);
        }
    }

    interface IVRDataProvider
    {
        event Action HeadsetTrackingStarted;
        event Action ModelLoaded;
        event Action<HandDirection, bool> HandConnectionChanged;

        bool Initialized { get; }

        bool IsStartupFrame { get; }

        bool Calibrating { get; }

        bool NeedsHeightCalibration { get; }

        float FixedDeltaTime { get; }

        bool ReadInputFromDevice(HandDirection handDir, HandInputType inputType);

        float ReadTriggerValue(HandDirection handDir);

        Vector2 ReadMoveInput(HandDirection handDir);

        void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude, HandDirection handDir);

        Transform GetModel(HandDirection handDir);

        Transform GetPoseTransform(HandDirection handDir);

        Collider GetControllerBounds(HandDirection handDir);

        void GetEstimatedPeakVelocities(HandDirection handDir, out Vector3 velocity, out Vector3 angular);

        Vector3 GetHandVelocity(HandDirection handDir);

        Vector3 GetHandAngularVelocity(HandDirection handDir);

        void GetBounds(out Vector3 firstCorner, out Vector3 secondCorner);

        void AdjustPlayerPositionBasedOnHeight(Transform playerTransform, float playerHeight, Vector3 startPos, float playAreaScale);

        float GetGrabAmount(HandDirection handDir);

        float GetFingerCurl(HandDirection handDir, Finger finger);
    }

    class NullDataProvider : IVRDataProvider
    {
        public bool Initialized => false;

        public bool IsStartupFrame => false;

        public bool Calibrating => false;

        public bool NeedsHeightCalibration => false;

        public float FixedDeltaTime => 1f / 72f;

        public event Action HeadsetTrackingStarted;
        public event Action ModelLoaded;
        public event Action<HandDirection, bool> HandConnectionChanged;

        public bool ReadInputFromDevice(HandDirection handDir, HandInputType inputType)
        {
            return false;
        }

        public Vector2 ReadMoveInput(HandDirection handDir)
        {
            return Vector2.zero;
        }

        public float ReadTriggerValue(HandDirection handDir)
        {
            return 0f;
        }

        public void Vibrate(float secondsFromNow, float duration, int vibrationFrequency, float amplitude, HandDirection handDir)
        {
        }

        public Transform GetModel(HandDirection handDir)
        {
            return null;
        }
        public Transform GetPoseTransform(HandDirection handDir)
        {
            return null;
        }

        public Collider GetControllerBounds(HandDirection handDir)
        {
            return null;
        }

        public void GetEstimatedPeakVelocities(HandDirection handDir, out Vector3 velocity, out Vector3 angular)
        {
            velocity = Vector3.zero;
            angular = Vector3.zero;
        }

        public Vector3 GetHandVelocity(HandDirection handDir)
        {
            return Vector3.zero;
        }

        public Vector3 GetHandAngularVelocity(HandDirection handDir)
        {
            return Vector3.zero;
        }

        public void GetBounds(out Vector3 firstCorner, out Vector3 secondCorner)
        {
            firstCorner = Vector3.zero;
            secondCorner = Vector3.zero;
        }

        public void AdjustPlayerPositionBasedOnHeight(Transform playerTransform, float playerHeight, Vector3 startPos, float playAreaScale)
        {
        }

        public float GetGrabAmount(HandDirection handDir)
        {
            return 0f;
        }

        public float GetFingerCurl(HandDirection handDir, Finger finger)
        {
            return 0f;
        }
    }
}
