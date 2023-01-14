using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionEvents : MonoBehaviour
{
    CollisionListener[] collisionListeners;

    void Awake()
    {
        collisionListeners = GetComponentsInChildren<CollisionListener>(true);
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach(var childListener in collisionListeners)
        {
            childListener.OnCollisionEnter(collision);
        }
    }
}

public interface CollisionListener
{
    void OnCollisionEnter(Collision collision);
}
