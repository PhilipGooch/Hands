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
        Animator puppetAnimator;
        [SerializeField]
        HandDirection direction;
        [SerializeField]
        [Range(0f, 1f)]
        float animationSpeed = 0.1f;
        [SerializeField]
        float scaleDuration = 0.25f;
        [SerializeField]
        SingleShotParticleEffect poofEffect;
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
        int threatId = Animator.StringToHash("Threat");
        int colorId = Shader.PropertyToID("_Color");
        int alphaMultiplierId = Shader.PropertyToID("_AlphaMultiplier");

        Color defaultHandColor;
        MaterialPropertyBlock propertyBlock;

        enum HandState
        {
            Hand,
            Puppet
        }

        HandState targetHandState = HandState.Hand;
        float stateSwitchProgress = 1f;
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
                bool inUI = PlayerUIManager.Instance.InteractingWithUI;
                float targetCurl = 0f;
                if (inUI)
                {
                    targetCurl = PointFinger();
                }
                else
                {
                    targetCurl = system.GetFingerCurl(direction, finger);
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
            defaultHandColor = handRenderer.sharedMaterial.GetColor(colorId);
            propertyBlock = new MaterialPropertyBlock();
            allFingers = new AnimatedFinger[] { thumb, index, middle, ring, pinky };
            if (!vrSystem.Initialized && direction == HandDirection.Left)
            {
                SetHandVisiblity(true);
            }
            vrSystem.HandConnectionChanged += UpdateHandConnection;
        }

        // Update is called once per frame
        void Update()
        {
            if (targetHand.IsThreat)
            {
                SwitchHandState(HandState.Puppet);
                puppetAnimator.SetFloat(threatId, targetHand.Trigger);
            }
            else
            {
                SwitchHandState(HandState.Hand);
                foreach (var finger in allFingers)
                {
                    finger.Update(handAnimator, vrSystem, targetHand, direction, animationSpeed);
                }
            }

            UpdateColor();
        }

        public bool IsPointing()
        {
            return IsPointing(vrSystem, direction, targetHand);
        }

        static bool IsPointing(VRSystem vrSystem, HandDirection direction, Hand hand)
        {
            var isGrabbingNothing = IsGrabbing(hand) && hand.attachedBody == null;
            return vrSystem.GetFingerCurl(direction, Finger.Index) < 0.2f && isGrabbingNothing;
        }

        static bool IsGrabbing(Hand targetHand)
        {
            return targetHand.Grab > 0f;
        }

        public bool IsEmptyFist()
        {
            return targetHand.Grab > grabAmountForFist && targetHand.attachedBody == null;
        }

        void UpdateHandConnection(HandDirection handDir, bool connected)
        {
            if (handDir == direction)
            {
                SetHandVisiblity(connected);
            }
        }

        void SwitchHandState(HandState state)
        {
            if (stateSwitchProgress < 1f)
            {
                stateSwitchProgress += Time.deltaTime / scaleDuration;
                if (stateSwitchProgress > 1f)
                {
                    stateSwitchProgress = 1f;
                }
            }

            if (targetHandState != state)
            {
                stateSwitchProgress = 1f - stateSwitchProgress;
                targetHandState = state;
                handAnimator.gameObject.SetActive(true);
                puppetAnimator.gameObject.SetActive(true);
                poofEffect?.Create(transform.position, transform.rotation);
            }

            var targetTransform = state == HandState.Hand ? handAnimator.transform : puppetAnimator.transform;
            var otherTransform = state == HandState.Hand ? puppetAnimator.transform : handAnimator.transform;

            targetTransform.localScale = Vector3.one * stateSwitchProgress;
            otherTransform.localScale = Vector3.one * (1f - stateSwitchProgress);

            if (stateSwitchProgress == 1f)
            {
                otherTransform.gameObject.SetActive(false);
            }
        }

        void UpdateColor()
        {
            var grabbingObject = targetHand.attachedBody != null;
            propertyBlock.SetFloat(alphaMultiplierId, grabbingObject ? grabAlphaMultiplier : 1f);
            handRenderer.SetPropertyBlock(propertyBlock);
        }

        void SetHandVisiblity(bool visible)
        {
            stateSwitchProgress = 1f;
            targetHandState = HandState.Hand;
            handAnimator.gameObject.SetActive(visible);
            puppetAnimator.gameObject.SetActive(visible);
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

