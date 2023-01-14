using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IGrabNotifications
{
    [SerializeField]
    [HideInInspector]
    ObjectDestroyer destroyer;

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            if (destroyer != null)
            {
                destroyer.SetDestuctionActive(false);
            }
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            if (destroyer != null)
            {
                destroyer.SetDestuctionActive(true);
            }
        }
    }

    void OnValidate()
    {
        if (destroyer == null)
        {
            destroyer = GetComponent<ObjectDestroyer>();
        }
    }
}
