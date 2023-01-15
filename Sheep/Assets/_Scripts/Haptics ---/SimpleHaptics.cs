using UnityEngine;

[System.Serializable]
public class SimpleHaptics// : IGrabNotifications
{
    [SerializeField]
    private float vibrationDelay = 0;
    [SerializeField]
    private float vibrationDuration = 0.1f;
    [SerializeField]
    private int vibrationFrequency = 10;
    [Range(0, 1)]
    [SerializeField]
    private float vibrationAmplitude = 0.5f;

    private Hand activeHand = null;

    public void Vibrate()
    {
        if (activeHand != null)
        {
            VibrateHand(activeHand);
        }
    }

    private void VibrateHand(Hand target)
    {
        target.Vibrate(vibrationDelay, vibrationDuration, vibrationFrequency, vibrationAmplitude);
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            activeHand = hand;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            activeHand = null;
        }
        else
        {
            activeHand = hand.otherHand;
        }
    }
}
