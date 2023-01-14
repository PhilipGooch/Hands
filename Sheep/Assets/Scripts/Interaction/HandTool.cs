using NBG.LogicGraph;
using System;
using UnityEngine;



//Hand tool is an object that can be activated with a hand trigger once grabbed
public class HandTool : MonoBehaviour, ITriggerHandler, IGrabNotifications
{
    [SerializeField]
    [Range(0f, 1f)]
    public float activationThreshold = 0.2f;
    [SerializeField]
    public bool requiresSnappedGrab = false;
    [SerializeField]
    public bool toggleActivation = false;

    GrabPositionAndRotationOverride grabOverride;
    bool canBeActivatedWithTrigger = false;
    float lastActivationPressure = 0f;

    public Hand MainHand => mainHand;
    protected Hand mainHand = null;

    public bool TwoHanded => twoHanded;
    protected bool twoHanded = false;

    [NodeAPI("OnActivationChange")]
    public event Action<float> OnActivationChange;

    protected virtual void Awake()
    {
        if (requiresSnappedGrab)
        {
            grabOverride = GetComponent<GrabPositionAndRotationOverride>();
            if (grabOverride == null)
            {
                Debug.LogError("RequiresSnappedGrab setting needs a GrabPositionAndRotationOverride component!", gameObject);
            }
        }

        OnActivationChange?.Invoke(0);
    }

    public void OnHandTrigger(float pressure)
    {
        if (canBeActivatedWithTrigger)
        {
            if (toggleActivation)
            {
                if (pressure > activationThreshold && lastActivationPressure < activationThreshold)
                {
                    OnActivationChange?.Invoke(pressure);
                }
                else if (pressure < activationThreshold && lastActivationPressure > activationThreshold)
                {
                    OnActivationChange?.Invoke(0f);
                }
            }
            else
            {
                if (pressure > activationThreshold)
                {
                    OnActivationChange?.Invoke(pressure);
                }
                else
                {
                    OnActivationChange?.Invoke(0f);
                }
            }
        }

        lastActivationPressure = pressure;
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            canBeActivatedWithTrigger = requiresSnappedGrab ? grabOverride.HandIsWithinSnappingZone(hand.transform.position, hand.rot) : true;
            mainHand = hand;
            twoHanded = false;
        }
        else
        {
            // No two handed operation
            canBeActivatedWithTrigger = false;
            OnActivationChange?.Invoke(0f);
            twoHanded = true;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            canBeActivatedWithTrigger = false;
            OnActivationChange?.Invoke(0f);
            mainHand = null;
        }
        else
        {
            // We released this hand, but the other hand might still be able to operate the tool
            canBeActivatedWithTrigger = requiresSnappedGrab ? grabOverride.HandIsWithinSnappingZone(hand.otherHand.transform.position, hand.otherHand.rot) : true;
            twoHanded = false;
            mainHand = hand.otherHand;
        }
    }
}
