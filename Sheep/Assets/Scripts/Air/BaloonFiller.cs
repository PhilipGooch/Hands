using NBG.Wind;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaloonFiller : MonoBehaviour, IWindReceiver
{
    [SerializeField]
    Balloon target;

    public void OnReceiveWind(Vector3 wind)
    {
        var dot = Vector3.Dot(transform.up.normalized, wind.normalized);
        if (dot > 0)
        {
            target.FillAir(dot);
        }
    }
}
