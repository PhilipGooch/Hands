using System;
using System.Collections.Generic;
using UnityEngine;

public class IsTouchedByGrabbedObj : MonoBehaviour
{
    public event Action onGrabbedTouchStarted;
    public event Action onGrabbedTouchEnded;

    [SerializeField]
    List<CollisionEventsSender> collisionEventsSenders;
    List<Rigidbody> grabbedObjs;
    List<Rigidbody> currentlyTouching;
    void Awake()
    {
        grabbedObjs = new List<Rigidbody>();
        currentlyTouching = new List<Rigidbody>();
        SubscribeToEvents();
    }

    void SubscribeToEvents()
    {
        Hand.onAttachObject += OnAttachObj;
        Hand.onDetachObject += OnDetachObj;

        for (int i = 0; i < collisionEventsSenders.Count; i++)
        {
            collisionEventsSenders[i].onCollisionEnter += CollisionEnter;
            collisionEventsSenders[i].onCollisionExit += CollisionExit;

        }
    }
    void CollisionEnter(Collision collision)
    {
        currentlyTouching.Add(collision.rigidbody);

        if (IsGrabbedObj(collision.rigidbody))
        {
            onGrabbedTouchStarted?.Invoke();
        }
    }
    void CollisionExit(Collision collision)
    {
        currentlyTouching.Remove(collision.rigidbody);


        if (IsGrabbedObj(collision.rigidbody))
        {
            onGrabbedTouchEnded?.Invoke();
        }
    }

    private void OnAttachObj(Rigidbody rigidbody)
    {
        if (currentlyTouching.Contains(rigidbody))
        {
            onGrabbedTouchStarted?.Invoke();
        }

        grabbedObjs.Add(rigidbody);
    }

    private void OnDetachObj(Rigidbody rigidbody)
    {
        if(currentlyTouching.Contains(rigidbody))
        {
            onGrabbedTouchEnded?.Invoke();
        }

        grabbedObjs.Remove(rigidbody);
    }

    public bool IsGrabbedObj(Rigidbody rigidbody)
    {
        var body = grabbedObjs.Find(x => x == rigidbody);

        return body != null;
    }
}
