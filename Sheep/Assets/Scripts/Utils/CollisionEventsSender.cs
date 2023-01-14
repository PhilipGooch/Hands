using System;
using UnityEngine;
// Utility for passing collision events to other monobehaviours

public class CollisionEventsSender : MonoBehaviour
{
    public Collider Collider { get; private set; }
    public event Action<Collision> onCollisionEnter;
    public event Action<Collision> onCollisionExit;
    public event Action<Collision> onCollisionStay;

    private void Awake()
    {
        Collider = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision other)
    {
        onCollisionEnter?.Invoke(other);
    }

    private void OnCollisionExit(Collision other)
    {

        onCollisionExit?.Invoke(other);
    }

    private void OnCollisionStay(Collision other)
    {
        onCollisionStay?.Invoke(other);
    }
}
