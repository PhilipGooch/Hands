using System;
using UnityEngine;

public class GrabEventsSender : MonoBehaviour, IGrabNotifications
{
    public event Action<Hand, bool> onGrab;
    public event Action<Hand, bool> onRelease;

    public void OnGrab(Hand hand, bool firstGrab)
    {
        onGrab?.Invoke(hand, firstGrab);
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        onRelease?.Invoke(hand, firstGrab);
    }
}
