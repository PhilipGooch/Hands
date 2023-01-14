using NBG.Wind;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleWindMultiplier : MonoBehaviour, IWindMultiplier
{
    [SerializeField]
    [Range(0f, 100f)]
    float windMultiplier = 1f;
    public float GetWindMultiplier(Vector3 windDirection)
    {
        return windMultiplier;
    }

    public void OnReceiveWind(Vector3 wind, Bounds windBounds)
    {

    }
}
