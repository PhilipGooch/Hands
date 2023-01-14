using UnityEngine;

public class HapticsBase : MonoBehaviour//, IGrabNotifications
{
    public bool active;
    [SerializeField]
    private SimpleHaptics haptics;
    [Tooltip("Optional, only add if you want to forward grab events from some other object")]
    //[SerializeField]
    //private GrabEventsSender eventsSender;

    protected virtual void Start()
    {
        //if (eventsSender != null)
        //{
        //    eventsSender.onGrab += OnGrab;
        //    eventsSender.onRelease += OnRelease;
        //}
    }

    public void TryVibrate()
    {
        if (active)
        {
            haptics.Vibrate();
        }
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        haptics.OnGrab(hand, firstGrab);
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        haptics.OnRelease(hand, firstGrab);
    }

    private void OnDestroy()
    {
        //if (eventsSender != null)
        //{
        //    eventsSender.onGrab -= OnGrab;
        //    eventsSender.onRelease -= OnRelease;
        //}
    }
}
