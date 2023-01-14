using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGrabNotifications { 
    void OnGrab(Hand hand, bool firstGrab);
    void OnRelease(Hand hand, bool firstGrab);
}
