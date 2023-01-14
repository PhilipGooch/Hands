using NBG.Wind;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sail : MonoBehaviour, IWindMultiplier
{
    [SerializeField]
    float maxWindMultiplier = 10f;

    public float GetWindMultiplier(Vector3 windDirection)
    {
        var dot = Vector3.Dot(transform.forward.normalized, windDirection.normalized);
        return maxWindMultiplier * Mathf.Abs(dot);
    }

    public void OnReceiveWind(Vector3 wind, Bounds windBounds)
    {

    }
}
