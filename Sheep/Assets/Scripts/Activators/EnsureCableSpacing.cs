using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnsureCableSpacing : MonoBehaviour
{
    [SerializeField]
    CableActivator firstCable;
    [SerializeField]
    CableActivator secondCable;

    float initialCableDistance;

    void Awake()
    {
        initialCableDistance = (firstCable.transform.position - secondCable.transform.position).magnitude;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var currentCableDiff = secondCable.GetCableConnectionPosition() - firstCable.GetCableConnectionPosition();
        var currentLength = currentCableDiff.magnitude;
        var currentExcess = currentLength - initialCableDistance;
        var halfExcess = currentExcess * 0.5f;
        var offset = currentCableDiff.normalized * halfExcess;
        firstCable.EndOffset = offset;
        secondCable.EndOffset = -offset;
    }
}
