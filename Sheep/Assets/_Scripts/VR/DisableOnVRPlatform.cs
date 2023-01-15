using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VR.System
{
    public class DisableOnVRPlatform : MonoBehaviour
    {
        [SerializeField]
        bool enabledOnSteamVR = true;
        [SerializeField]
        bool enabledOnOculus = true;

        private void Awake()
        {
            var shouldBeEnabled = true;
            switch (VRSystem.CurrentVRPlatform)
            {
                case VRPlatform.SteamVR:
                    shouldBeEnabled = enabledOnSteamVR;
                    break;
                case VRPlatform.Oculus:
                    shouldBeEnabled = enabledOnOculus;
                    break;
            }

            if (!shouldBeEnabled)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
