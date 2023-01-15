using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VR.System
{
    public class HandAnimator : MonoBehaviour
    {
        [SerializeField]
        VRSystem vrSystem;
        [SerializeField]
        Animator handAnimator;
        [SerializeField]
        HandDirection direction;
        [SerializeField]
        [Range(0f, 1f)]
        float animationSpeed = 0.1f;
        [SerializeField]
        float scaleDuration = 0.25f;
        [SerializeField]
        Hand targetHand;
        [SerializeField]
        SkinnedMeshRenderer handRenderer;
        [SerializeField]
        float grabAlphaMultiplier = 0.5f;

        AnimatedFinger thumb = new AnimatedFinger("Thumb_Curl", Finger.Thumb);
        AnimatedFinger index = new AnimatedFinger("Index_Curl", Finger.Index);
        AnimatedFinger middle = new AnimatedFinger("Middle_Curl", Finger.Middle);
        AnimatedFinger ring = new AnimatedFinger("Ring_Curl", Finger.Ring);
        AnimatedFinger pinky = new AnimatedFinger("Pinky_Curl", Finger.Pinky);
        int alphaMultiplierId = Shader.PropertyToID("_AlphaMultiplier");

        MaterialPropertyBlock propertyBlock;

        const float grabAmountForFist = 0.8f;

        AnimatedFinger[] allFingers;

        class AnimatedFinger
        {
            int fingerCurlId;
            Finger finger;
            float curlValue;

            public AnimatedFinger(string id, Finger finger)
            {
                fingerCurlId = Animator.StringToHash(id);
                this.finger = finger;
                curlValue = 0f;
            }

            public void Update(Animator animator, VRSystem system, Hand hand, HandDirection direction, float speed)
            {
                float targetCurl = system.GetFingerCurl(direction, finger);
                bool isPointing = IsPointing(system, direction, hand);
                bool isGrabbing = IsGrabbing(hand);
                if (isPointing)
                {
                    targetCurl = PointFinger();
                }
                else if (isGrabbing)
                {
                    targetCurl = Mathf.Max(targetCurl, hand.Grab);
                }
                curlValue = Mathf.MoveTowards(curlValue, targetCurl, speed);
                animator.SetFloat(fingerCurlId, curlValue);
            }

            float PointFinger()
            {
                if (finger == Finger.Index || finger == Finger.Thumb)
                {
                    return 0f;
                }
                else
                {
                    return 1f;
                }
            }
        }

        private void Start()
        {
            propertyBlock = new MaterialPropertyBlock();
            allFingers = new AnimatedFinger[] { thumb, index, middle, ring, pinky };
            if (!vrSystem.Initialized && direction == HandDirection.Left)
            {
                SetHandVisiblity(true);
            }
            vrSystem.HandConnectionChanged += UpdateHandConnection;
        }

        void Update()
        {
            foreach (var finger in allFingers)
            {
                finger.Update(handAnimator, vrSystem, targetHand, direction, animationSpeed);
            }
            UpdateColor();
        }

        public bool IsPointing()
        {
            return IsPointing(vrSystem, direction, targetHand);
        }

        static bool IsPointing(VRSystem vrSystem, HandDirection direction, Hand hand)
        {
            return vrSystem.GetFingerCurl(direction, Finger.Index) < 0.2f && IsGrabbing(hand);
        }

        static bool IsGrabbing(Hand targetHand)
        {
            return targetHand.Grab > 0f;
        }


        void UpdateHandConnection(HandDirection handDir, bool connected)
        {
            if (handDir == direction)
            {
                SetHandVisiblity(connected);
            }
        }

        void UpdateColor()
        {
            propertyBlock.SetFloat(alphaMultiplierId, 1f);
            handRenderer.SetPropertyBlock(propertyBlock);
        }

        void SetHandVisiblity(bool visible)
        {
            handAnimator.gameObject.SetActive(visible);
        }

        private void OnValidate()
        {
            if (!vrSystem)
            {
                vrSystem = GetComponentInParent<VRSystem>();
            }
            if (!targetHand)
            {
                targetHand = GetComponentInParent<Hand>();
            }
        }
    }
}

