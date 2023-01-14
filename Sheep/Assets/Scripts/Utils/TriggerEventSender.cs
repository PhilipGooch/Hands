using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Utility for passing trigger events to other monobehaviours
public class TriggerEventSender : MonoBehaviour
{
    public Collider Collider {get; private set;}
    public event Action<Collider> onTriggerEnter;
    public event Action<Collider> onTriggerExit;
    public event Action<Collider> onTriggerStay;

    private void Awake()
    {
        Collider = GetComponent<Collider>();
    }

    public void OnTriggerEnter(Collider other)
    {
        onTriggerEnter?.Invoke(other);
    }

    public void OnTriggerExit(Collider other)
    {
        onTriggerExit?.Invoke(other);   
    }

    public void OnTriggerStay(Collider other)
    {
        onTriggerStay?.Invoke(other);   
    }
}
