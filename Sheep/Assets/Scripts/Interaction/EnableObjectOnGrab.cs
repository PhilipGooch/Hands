using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableObjectOnGrab : MonoBehaviour, IGrabNotifications
{
    [SerializeField]
    GameObject target;
    [SerializeField]
    bool inverted = false;

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            target.SetActive(inverted ? false : true);
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            target.SetActive(inverted ? true : false);
        }    
    }
}
