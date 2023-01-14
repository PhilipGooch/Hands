using Plugs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Plugs;

public class ElectricCord : MonoBehaviour
{
    [SerializeField]
    Plug firstPlug;
    [SerializeField]
    Plug secondPlug;

    Dictionary<Plug, HoleSocket> connections = new Dictionary<Plug, HoleSocket>();

    private void Awake()
    {
        SetupPlug(firstPlug);
        SetupPlug(secondPlug);
    }

    void SetupPlug(Plug plug)
    {
        connections[plug] = null;
        plug.onPlugIn += (hole) => OnPlugIn(hole, plug);
        plug.onPlugOut += (x) => OnPlugOut(plug);
    }

    void OnPlugIn(Hole hole, Plug plug)
    {
        connections[plug] = hole.GetComponent<HoleSocket>();
    }

    void OnPlugOut(Plug plug)
    {
        var input = connections[plug];
        if (!input.IsActiveSocket)
        {
            input.Power = 0f;
        }
        connections[plug] = null;
    }

    private void FixedUpdate()
    {
        var firstConnection = connections[firstPlug];
        var secondConnection = connections[secondPlug];
        if (firstConnection != null || secondConnection != null)
        {
            TransferSignal(firstConnection, secondConnection);
        }
    }

    void TransferSignal(HoleSocket first, HoleSocket second)
    {
        float power = Mathf.Max(GetActivePower(first), GetActivePower(second));
        SetPassivePower(first, power);
        SetPassivePower(second, power);
    }

    float GetActivePower(HoleSocket socket)
    {
        if (socket != null && socket.IsActiveSocket)
        {
            return socket.Power;
        }
        return 0f;
    }

    void SetPassivePower(HoleSocket socket, float power)
    {
        if (socket != null && !socket.IsActiveSocket)
        {
            socket.Power = power;
        }
    }
}
